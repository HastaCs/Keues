namespace Keues.Domain.Events;

public class TicketTransferred:IKeuesEvent
{
  public DateTime OccurredOn { get; }= DateTime.UtcNow;
  
  public Guid Id { get; }
  public Guid FromCounterId { get; }
  public Guid ToCounterId { get; }
  
  public TicketTransferred(Guid ticketId, Guid fromCounterId, Guid toCounterId)
  {
    Id = ticketId;
    FromCounterId = fromCounterId;
    ToCounterId = toCounterId;
  }
}