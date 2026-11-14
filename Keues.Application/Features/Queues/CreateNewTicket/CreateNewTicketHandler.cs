using System.Collections.Concurrent;
using Keues.Application.Common;
using Keues.Domain.Entities;
using Keues.Application.Events;
using Keues.Domain.Events;

namespace Keues.Application.Features.Queues.CreateNewTicket;

public class CreateNewTicketHandler
{
  // Serializa la numeracion de cada cola dentro del proceso. El despliegue actual
  // usa un unico contenedor; si se escala horizontalmente, habria que complementarlo
  // con concurrencia a nivel de base de datos.
  private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> QueueLocks = new();

  private readonly IApplicationDbContext _context;
  private readonly IKeuesEventPublisher _eventPublisher;

  public CreateNewTicketHandler(IApplicationDbContext context, IKeuesEventPublisher eventPublisher)
  {
    _context = context;
    _eventPublisher = eventPublisher;
  }

  public async Task<CreateNewTicketResponse> Handle(CreateNewTicketCommand request)
  {
    var gate = QueueLocks.GetOrAdd(request.QueueId, _ => new SemaphoreSlim(1, 1));
    Guid ticketId;
    string ticketCode;

    await gate.WaitAsync();
    try
    {
      var queue = await _context.Queues.FindAsync(request.QueueId);
      if (queue == null)
      {
        throw new Exception("Ticket type not found");
      }

      // Fuerza una lectura fresca de NextNumber. La cola puede estar ya cargada
      // en el contexto por una consulta anterior, como QrFlowResolver.
      await _context.Queues.Entry(queue).ReloadAsync();

      var ticket = queue.CreateNewTicket(request.FlowId);
      _context.Tickets.Add(ticket);

      var history = new TicketHistory
      {
        TicketId = ticket.Id,
        Event = EventTypes.Ticket.Created,
        Counter = null,
        QueueId = queue.Id,
      };
      await _context.TicketHistories.AddAsync(history);
      await _context.SaveChangesAsync();

      ticketId = ticket.Id;
      ticketCode = ticket.Code;
    }
    finally
    {
      gate.Release();
    }

    await _eventPublisher.Publish(new TicketCreated(ticketId), CancellationToken.None);

    return new CreateNewTicketResponse(ticketId, ticketCode);
  }
}