namespace Keues.Domain.Events;

public class TicketCalled:IKeuesEvent
{
  public DateTime OccurredOn { get; }= DateTime.UtcNow;

  public Guid Id { get; }
  public Guid CounterId { get; }

  public TicketCalled(Guid ticketId, Guid counterId)
  {
    Id = ticketId;
    CounterId = counterId;
  }
}