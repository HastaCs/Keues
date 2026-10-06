using Keues.Domain.Events;

namespace Keues.Application.Events;

public interface IKeuesEventDispatcher
{
  Task DispatchAsync(IKeuesEvent keuesEvent, CancellationToken cancellationToken = default);
}