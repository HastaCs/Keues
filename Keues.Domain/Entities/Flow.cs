using System.ComponentModel.DataAnnotations;
using Keues.Domain.Enums;

namespace Keues.Domain.Entities;

public class Flow
{
  [Key]
  public Guid Id { get; set; }=Guid.NewGuid();

  public string Name { get; set; } = string.Empty;
  
  public string Description { get; set; } = string.Empty;

  public FlowType FlowType { get; set; }

 
  public DateTime? RemovedAt { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  
  public Guid LocationId { get;  set; }
  public Location Location { get; set; } = null!;

  public string FlowJson { get; set; } = "{}";
}