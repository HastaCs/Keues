namespace Keues.Domain.Events;

public class TicketAttended: IKeuesEvent
{
  public DateTime OccurredOn { get; }= DateTime.UtcNow;
  public Guid Id { get; }
  // The ID of the counter that attended the ticket
  public Guid CounterId { get; }
  
  public TicketAttended(Guid ticketId, Guid counterId)
  {
    Id = ticketId;
    CounterId = counterId;
  }
}