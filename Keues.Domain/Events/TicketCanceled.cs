namespace Keues.Domain.Events;

public class TicketCanceled:IKeuesEvent
{
  public DateTime OccurredOn { get; }= DateTime.UtcNow;
  public string EventType { get; set; } = EventTypes.Ticket.Canceled;
  public Guid Id { get; }

  public TicketCanceled(Guid ticketId)
  {
    Id = ticketId;
  }
}