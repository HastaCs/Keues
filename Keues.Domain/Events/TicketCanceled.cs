namespace Keues.Domain.Events;

public class TicketCanceled : IKeuesEvent
{
  public DateTime OccurredOn { get; } = DateTime.UtcNow;
  public string EventType { get; set; } = EventTypes.Ticket.Canceled;
  public Guid Id { get; }
  public Guid CounterId { get; }
  public Guid? UserId { get; }

  public TicketCanceled(Guid ticketId, Guid counterId, Guid? userId = null)
  {
    Id = ticketId;
    CounterId = counterId;
    UserId = userId;
  }
}
