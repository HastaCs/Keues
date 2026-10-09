using Keues.Application.Common;
using Keues.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Events;

public class TicketEventPayloadBuilder(IApplicationDbContext context)
{
  public async Task<TicketEventPayload> BuildAsync(
    string eventType,
    DateTime occurredAt,
    Guid ticketId,
    Guid? eventCounterId,
    Guid? userId,
    CancellationToken cancellationToken = default)
  {
    // Una sola query: ticket + cola + puesto + flujo->localización
    var ticket = await context.Tickets
      .Include(t => t.Queue)
      .Include(t => t.Counter)
      .Include(t => t.Flow).ThenInclude(f => f.Location)
      .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

    if (ticket is null)
    {
      return new TicketEventPayload(
        eventType,
        occurredAt,
        new TicketEventData(
          ticketId, string.Empty,
          new EntityRef(Guid.Empty, string.Empty),
          null, null,
          new EntityRef(Guid.Empty, string.Empty)));
    }

    var queue = new EntityRef(ticket.Queue.Id, ticket.Queue.Name);

    EntityRef? counter = null;
    if (ticket.Counter is not null)
    {
      counter = new EntityRef(ticket.Counter.Id, ticket.Counter.Name);
    }
    else if (eventCounterId is Guid counterId)
    {
      counter = await ResolveCounter(counterId, cancellationToken);
    }

    EntityRef? user = null;
    if (userId is Guid userIdValue)
    {
      user = await ResolveUser(userIdValue, cancellationToken);
    }

    var location = new EntityRef(ticket.Flow.Location.Id, ticket.Flow.Location.Name);

    var data = new TicketEventData(ticket.Id, ticket.Code, queue, counter, user, location);
    return new TicketEventPayload(eventType, occurredAt, data);
  }

  private async Task<EntityRef?> ResolveCounter(Guid counterId, CancellationToken cancellationToken)
  {
    var counter = await context.Counters.FindAsync([counterId], cancellationToken);
    return counter is null ? null : new EntityRef(counter.Id, counter.Name);
  }

  private async Task<EntityRef?> ResolveUser(Guid userId, CancellationToken cancellationToken)
  {
    var user = await context.Users.FindAsync([userId], cancellationToken);
    return user is null ? null : new EntityRef(user.Id, user.Name);
  }
}
