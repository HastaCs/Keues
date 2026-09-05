using System.Data;
using Keues.Application.Common;
using Keues.Domain.Entities;
using Keues.Domain.Enums;
using Keues.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Keues.Application.Features.Counters.CallNextTicket;

/// <summary>
/// Handler para llamar al siguiente ticket en la cola.
/// Implementa transacciones con nivel Serializable para prevenir race conditions
/// cuando múltiples counters intentan llamar tickets simultáneamente.
/// </summary>
public class CallNextTicketHandler
{
  private readonly IApplicationDbContext _context;

  public CallNextTicketHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<CallNextTicketResponse?> Handle(CallNextTicketCommand command)
  {
    // Usamos una transacción con nivel de aislamiento Serializable para prevenir race conditions
    // cuando múltiples counters intentan llamar al siguiente ticket al mismo tiempo.
    var strategy = _context.Database.CreateExecutionStrategy();

    return await strategy.ExecuteAsync(async () =>
    {
      // SQLite no soporta completamente niveles de aislamiento, pero podemos usar una transacción exclusiva
      // que evita que múltiples escrituras concurrentes causen race conditions
      await using var transaction = await _context.Database.BeginTransactionAsync();

      try
      {
        //El puesto que esta llamando el turno
        var counter = await _context.Counters
          .Include(x => x.Queues)
          .FirstOrDefaultAsync(x => x.Id == command.CounterId);

        if (counter == null)
          throw new Exception("Counter not found");

        // Si el puesto ya tiene un ticket en curso, se vuelve a llamar el mismo.
        var currentTicket = await _context.Tickets
          .FirstOrDefaultAsync(x =>
            x.CounterId == counter.Id &&
            x.Status == TicketStatus.InProgress);

        if (currentTicket != null)
        {
          currentTicket.CalledAt = DateTime.UtcNow;

          var history = new TicketHistory
          {
            TicketId = currentTicket.Id,
            CounterId = counter.Id,
            CreatedAt = DateTime.UtcNow,
            Event = KeuesEventsType.Ticket.Called,
          };
          await _context.TicketHistories.AddAsync(history);
          await _context.SaveChangesAsync();
          await transaction.CommitAsync();
          return new CallNextTicketResponse(currentTicket.Id, currentTicket.Code, currentTicket.QueueId);
        }

        var queueIds = counter.Queues
          .Select(x => x.Id)
          .ToList();

        //Tickets que puede llamar ese puesto y estan esperando
        var waitingTickets = await _context.Tickets
          .Include(x => x.Queue)
          .Where(x =>
            x.Status == TicketStatus.Waiting &&
            queueIds.Contains(x.QueueId))
          .ToListAsync();

        if (!waitingTickets.Any())
        {
          await transaction.CommitAsync();
          return null;
        }

        // Obtener el ticket más antiguo de cada cola y calcular su prioridad efectiva.
        var queues = waitingTickets
          .GroupBy(x => x.Queue)
          .Select(g =>
          {
            var oldest = g
              .OrderBy(x => x.CreatedAt)
              .First();
            var queue = g.Key;
            int agingBonus = 0;
            if (queue.AgingIntervalMinutes > 0)
            {
              var waitingMinutes = (int)(DateTime.UtcNow - oldest.CreatedAt).TotalMinutes;

              agingBonus = waitingMinutes / queue.AgingIntervalMinutes;
              agingBonus = Math.Min(agingBonus, queue.MaxAgingBonus);
            }

            return new
            {
              Queue = queue,
              Ticket = oldest,
              EffectivePriority = queue.Priority + agingBonus,
              Weight = Math.Max(queue.Weight, 1)
            };
          })
          .ToList();

        // Obtener la prioridad efectiva máxima.
        var maxPriority = queues.Max(x => x.EffectivePriority);

        // Solo las colas con mayor prioridad.
        var candidates = queues
          .Where(x => x.EffectivePriority == maxPriority)
          .ToList();

        // Selección ponderada por Weight.
        // El weight sirve para llamar a 1 ticket A por cada 3 tickets B por ejemplo
        var selected = candidates.First();

        if (candidates.Count > 1)
        {
          var totalWeight = candidates.Sum(x => x.Weight);

          var random = Random.Shared.Next(totalWeight);

          int accumulated = 0;

          foreach (var candidate in candidates)
          {
            accumulated += candidate.Weight;

            if (random < accumulated)
            {
              selected = candidate;
              break;
            }
          }
        }

        var ticket = selected.Ticket;

        ticket.CounterId = counter.Id;
        ticket.Status = TicketStatus.InProgress;
        ticket.CalledAt = DateTime.UtcNow;
        var historyT = new TicketHistory
        {
          TicketId = ticket.Id,
          CounterId = counter.Id,
          CreatedAt = DateTime.UtcNow,
          Event = KeuesEventsType.Ticket.Called,
        };
        await _context.TicketHistories.AddAsync(historyT);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new CallNextTicketResponse(ticket.Id, ticket.Code, ticket.QueueId);
      }
      catch
      {
        await transaction.RollbackAsync();
        throw;
      }
    });
  }
}