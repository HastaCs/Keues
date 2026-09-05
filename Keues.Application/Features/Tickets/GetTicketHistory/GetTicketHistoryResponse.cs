namespace Keues.Application.Features.Tickets.GetTicketHistory;

public class GetTicketHistoryResponse
{
  public Guid Id { get; set; }
  public string Event { get; set; }
  public DateTime CreatedAt { get; set; }
  public string CounterName { get; set; }
  
}