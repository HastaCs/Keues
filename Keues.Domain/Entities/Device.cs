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

  /// <summary>
  /// Para indicar en el dispositivo si esta activo, si una maquina de tickets no esta activa saldra  en pantalla como no disponible
  /// </summary>
  public bool IsActive { get; set; } = true;

}