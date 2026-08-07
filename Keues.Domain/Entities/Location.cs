namespace Keues.Domain.Entities;

public class Location
{
 public Guid Id { get; set; } 

  public string Name { get;  set; } 

  public string? Description { get;  set; }

  public DateTime? RemovedAt { get;  set; }
  
  public DateTime CreatedAt { get;  set; } = DateTime.UtcNow;

  public ICollection<Queue> Queues { get;  set; } = new List<Queue>();

  public ICollection<Counter> Counters { get;  set; } = new List<Counter>();
  
  public ICollection<Flow> Flows { get;  set; } = new List<Flow>();
  
  public ICollection<Device> Devices { get;  set; } = new List<Device>();

  public string? Color { get; set; } = "blue";
 
  private Location() { }

  public static Location Create(string name, string description,string color)
  {
    return new Location()
    {
      Name = name,
      Description = description,
      Color = color
    };
  }
  
}