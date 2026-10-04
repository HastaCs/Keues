using Keues.Domain.Events;

namespace Keues.Application.Events;

public interface IKeuesEventPublisher
{
  Task Publish<TEvent>(TEvent keuesEvent, CancellationToken cancellationToken = default) where TEvent : IKeuesEvent;
}