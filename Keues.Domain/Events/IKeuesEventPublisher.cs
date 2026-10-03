namespace Keues.Domain.Events;

public interface IKeuesEventPublisher
{
  Task Publish<TEvent>(TEvent keuesEvent, CancellationToken cancellationToken = default) where TEvent : IKeuesEvent;
}