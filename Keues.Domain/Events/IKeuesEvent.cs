namespace Keues.Domain.Events;

public interface IKeuesEvent
{
  public DateTime OccurredOn { get;  }
  public string EventType { get; set; }
}