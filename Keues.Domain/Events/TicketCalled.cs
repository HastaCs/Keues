namespace Keues.Domain.Events;

public class TicketCalled : IKeuesEvent
{
  public DateTime OccurredOn { get; } = DateTime.UtcNow;
  public string EventType { get; set; } = EventTypes.Ticket.Called;
  public Guid Id { get; }
  public Guid CounterId { get; }
  public Guid? UserId { get; }

  public TicketCalled(Guid ticketId, Guid counterId, Guid? userId = null)
  {
    Id = ticketId;
    CounterId = counterId;
    UserId = userId;
  }
}
