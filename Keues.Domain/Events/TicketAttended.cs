namespace Keues.Domain.Events;

public class TicketAttended : IKeuesEvent
{
  public DateTime OccurredOn { get; } = DateTime.UtcNow;
  public string EventType { get; set; } = EventTypes.Ticket.Attended;
  public Guid Id { get; }
  public Guid CounterId { get; }
  public Guid? UserId { get; }

  public TicketAttended(Guid ticketId, Guid counterId, Guid? userId = null)
  {
    Id = ticketId;
    CounterId = counterId;
    UserId = userId;
  }
}
