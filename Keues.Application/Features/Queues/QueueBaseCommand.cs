namespace Keues.Application.Features.Queues;

public record QueueBaseCommand
{
  public string Name { get; set; }
  public string Description { get; set; }
  public string Code { get; set; }

  public int? MaxValue { get; set; }

  public Guid LocationId { get; set; }

  public IEnumerable<Guid>? Counters { get; set; }

  public int Priority { get; set; }
  public int Weight { get; set; }
  public int AgingIntervalMinutes { get; set; }
  public int MaxAgingBonus { get; set; }
  public string Color { get; set; }
}