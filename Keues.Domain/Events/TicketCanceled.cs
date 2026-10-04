namespace Keues.Domain.Events;

public class TicketCanceled:IKeuesEvent
{
  public DateTime OccurredOn { get; }= DateTime.UtcNow;
  public Guid Id { get; }

  public TicketCanceled(Guid ticketId)
  {
    Id = ticketId;
  }
}