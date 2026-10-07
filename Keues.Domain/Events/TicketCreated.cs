namespace Keues.Domain.Events;

public class TicketCreated : IKeuesEvent
{
  public DateTime OccurredOn { get; } = DateTime.UtcNow;
  public string EventType { get; set; } = EventTypes.Ticket.Created;
  public Guid Id { get; } 

  public TicketCreated(Guid TicketId)
  {
    Id = TicketId;
  }
}