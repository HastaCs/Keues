using Keues.Domain.Events;

namespace Keues.Tests.Infrastructure;

public class FakeEventPublisher : IKeuesEventPublisher
{
  public List<IKeuesEvent> PublishedEvents { get; } = [];

  public Task Publish<TEvent>(TEvent keuesEvent, CancellationToken cancellationToken = default)
    where TEvent : IKeuesEvent
  {
    PublishedEvents.Add(keuesEvent);
    return Task.CompletedTask;
  }
}
