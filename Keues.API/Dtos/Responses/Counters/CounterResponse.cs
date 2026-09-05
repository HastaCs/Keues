namespace Keues.API.Responses.Counters;

public class CounterResponse
{
  public Guid Id { get; init; }
  public string Name { get; init; }
  public string Code { get; init; }
  public string? Description { get; init; }
  public string? Color { get; init; }
  public IEnumerable<Guid> Queues { get; init; }
  public Guid LocationId { get; init; }
  public DateTime CreatedAt { get; init; }
}
