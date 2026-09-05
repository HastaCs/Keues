namespace Keues.API.Requests.Counters;

public class UpdateCounterRequest
{
  public string Code { get; set; }
  public string Color { get; set; }
  public string Name { get; set; }
  public string Description { get; set; }
  public Guid LocationId { get; set; }

  /// <summary>
  /// Colas a las que tiene acceso este counter
  /// </summary>
  public IEnumerable<Guid>? Queues { get; set; }
}