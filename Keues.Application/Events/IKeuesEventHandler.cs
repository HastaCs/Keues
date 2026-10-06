using Keues.Domain.Events;

namespace Keues.Application.Events;

public interface IKeuesEventHandler<in TEvent> where TEvent : IKeuesEvent
{
  Task HandleAsync(TEvent keuesEvent, CancellationToken cancellationToken = default);
}