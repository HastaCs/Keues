using Keues.Domain.Entities;
using Keues.Domain.Enums;
using Keues.Infrastructure.Persistence;

namespace Keues.Tests.Infrastructure;

/// <summary>
/// Helpers para sembrar datos de prueba en un AppDbContext.
/// </summary>
public static class Seed
{
  public static async Task<Location> LocationAsync(
    AppDbContext context,
    string name = "Tienda Central",
    string color = "blue")
  {
    var location = Location.Create(name, "Descripción de la localización", color);
    context.Locations.Add(location);
    await context.SaveChangesAsync();
    return location;
  }

  public static async Task<Flow> FlowAsync(
    AppDbContext context,
    Guid locationId,
    FlowType type = FlowType.TicketMachine,
    string name = "Flujo principal")
  {
    var flow = new Flow
    {
      Name = name,
      Description = "",
      FlowType = type,
      LocationId = locationId,
      FlowJson = "{}"
    };
    context.Flows.Add(flow);
    await context.SaveChangesAsync();
    return flow;
  }

  public static async Task<Queue> QueueAsync(
    AppDbContext context,
    Guid locationId,
    string code = "Q",
    string name = "Cola",
    int priority = 0,
    int weight = 1,
    int agingIntervalMinutes = 0,
    int maxAgingBonus = 0)
  {
    var queue = Queue.Create(name, code, null, "Descripción", locationId, priority, weight,
      agingIntervalMinutes, maxAgingBonus, "blue");
    context.Queues.Add(queue);
    await context.SaveChangesAsync();
    return queue;
  }

  public static async Task<Counter> CounterAsync(
    AppDbContext context,
    Guid locationId,
    string code = "C1",
    string name = "Caja 1",
    IEnumerable<Queue>? queues = null)
  {
    var counter = Counter.Create(name, "", code, "green", locationId);
    if (queues != null)
    {
      foreach (var queue in queues)
      {
        counter.Queues.Add(queue);
      }
    }

    context.Counters.Add(counter);
    await context.SaveChangesAsync();
    return counter;
  }

  public static async Task<Ticket> TicketAsync(
    AppDbContext context,
    Guid queueId,
    Guid flowId,
    DateTime? createdAt = null,
    TicketStatus? status = null)
  {
    var queue = await context.Queues.FindAsync(queueId);
    var ticket = queue!.CreateNewTicket(flowId);

    if (createdAt.HasValue)
    {
      ticket.CreatedAt = createdAt.Value;
    }

    if (status.HasValue)
    {
      ticket.Status = status.Value;
    }

    context.Tickets.Add(ticket);
    await context.SaveChangesAsync();
    return ticket;
  }
}
