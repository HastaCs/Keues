using System.ComponentModel.DataAnnotations;

namespace Keues.Domain.Entities;

public class TicketHistory
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public Guid TicketId { get; set; }
  public Ticket Ticket { get; set; } = null!;
  
  public string Event { get; set; } = null!;
  
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  
  public Guid? CounterId { get; set; }
  public Counter? Counter { get; set; } 
  
}