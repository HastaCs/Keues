using System.ComponentModel.DataAnnotations;

namespace Keues.Domain.Entities;

public class Counter
{
  [Key] public Guid Id { get; set; }

  public string Color { get; set; } 

  /// <summary>
  /// Codigo en el monitor de turnos
  /// Por ejemplo P: Pescaderia  C:Carniceria
  /// </summary>
  public string Code { get; set; }

  /// <summary>
  /// Para mostrarlo en el dashboard
  /// </summary>
  public string Name { get; set; } 

  public string Description { get; set; } = "";

  public Guid LocationId { get; set; }
  public Location Location { get; set; } = null!;

  public DateTime? RemovedAt { get; set; }
  public DateTime? CreatedAt { get; set; } 

  /// <summary>
  /// Colas a las que puede llamar, para no llamar turnos de otros puestos.
  /// </summary>
  public ICollection<Queue> Queues { get; set; } = [];

  public static Counter Create(string name, string description, string code, string color,Guid locationId)
  {
    return new Counter()
    {LocationId = locationId,
      Name = name,
      Description = description,
      Code = code,
      Color = color,
      CreatedAt = DateTime.UtcNow
    };
  }
}