namespace Keues.Domain.Entities;

public class UserGroup
{
  public Guid Id { get; set; }
  
  public string Color { get; set; } = "blue";
  public string Name { get; set; } = string.Empty;
  
  public Guid LocationId { get; set; }
  public Location Location { get; set; }
  
  public DateTime? RemovedAt { get; set; }
  
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  
  public ICollection<User> Users { get; set; } = new List<User>();
}