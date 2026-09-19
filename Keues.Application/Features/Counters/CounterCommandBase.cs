namespace Keues.Application.Features.Counters;

public abstract record CounterCommandBase
{
  public string Code { get; set; }
  public string Color { get; set; }
  public string Name { get; set; }
  public string Description { get; set; }
  public Guid LocationId { get; set; }
  
  /// <summary>
  /// Usuarios que tienen acceso a este counter
  /// </summary>
  public IEnumerable<Guid>? AuthorizedUsers { get; set; }
  /// <summary>
  /// Grupos de usuarios que tienen acceso a este counter
  /// </summary>
  public IEnumerable<Guid>? AuthorizedUserGroups { get; set; }

  /// <summary>
  /// Colas a las que tiene acceso este counter
  /// </summary>
  public IEnumerable<Guid>? Queues { get; set; }
}