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
 
}