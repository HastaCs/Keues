using Keues.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Events.Handlers;

public class KeuesEventDispatcher:IKeuesEventDispatcher
{
  private readonly IServiceProvider _serviceProvider;
  public KeuesEventDispatcher(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
  }
  public async Task DispatchAsync(IKeuesEvent keuesEvent, CancellationToken cancellationToken = default)
  {
    var handlerType = typeof(IKeuesEventHandler<>)
      .MakeGenericType(keuesEvent.GetType());

    dynamic handler = _serviceProvider.GetRequiredService(handlerType);

    await handler.HandleAsync((dynamic)keuesEvent, cancellationToken);
  }
}