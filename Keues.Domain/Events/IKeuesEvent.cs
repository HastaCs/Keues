namespace Keues.Domain.Events;

public interface IKeuesEvent
{
  public DateTime OccurredOn { get;  }
}