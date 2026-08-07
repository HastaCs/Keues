namespace Keues.Application.Features.Counters.GetAllCounters;

public record GetAllCountersCommand
{
  public Guid? LocationId { get; init; }
}