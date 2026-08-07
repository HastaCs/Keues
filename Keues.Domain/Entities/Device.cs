using System.ComponentModel.DataAnnotations;
using Keues.Domain.Enums;

namespace Keues.Domain.Entities;

public class Device
{
  [Key]
  public Guid Id { get; set; }
  
  public  string Name { get; set; }
  public DeviceType Type { get; set; }
  public DateTime LastConnection { get; set; }
  
  public Guid LocationId { get; set; }
  public Location Location { get;  set; } = null!;
  
  public DateTime CreatedAt { get; set; }
  
}