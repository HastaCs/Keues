using System.ComponentModel.DataAnnotations;
using Keues.Domain.Enums;

namespace Keues.Domain.Entities;

public class User
{
  [Key]
  public Guid Id { get; set; }=Guid.NewGuid();
  
  public string Name { get; set; } = string.Empty;

  public string Email { get; set; }
  
  public string PasswordHash { get; set; }
  
  public Rol Role { get; set; }
  
  //El admin es null
  public Guid? LocationId { get; set; }
  public Location? Location { get; set; }
  
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  
  public DateTime? RemovedAt { get; set; }
  
  public Boolean Enabled { get; set; } = true;
  //ExternalId is used to link the user with an external system, like an identity provider or a third-party service.
  public string ExternalId { get; set; } = string.Empty;
  
 public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
 
 public ICollection<Counter> AuthorizedCounters { get; set; } = new List<Counter>();
}