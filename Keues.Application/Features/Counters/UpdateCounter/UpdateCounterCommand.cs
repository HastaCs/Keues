namespace Keues.Application.Features.Counters.UpdateCounter;

public record UpdateCounterCommand : CounterCommandBase
{
  public Guid Id { get; set; }
  
}