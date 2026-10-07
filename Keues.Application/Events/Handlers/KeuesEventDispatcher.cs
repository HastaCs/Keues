using Keues.Application.Features.WebhooksConfig.IsSubscribed;
using Keues.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Events.Handlers;

public class KeuesEventDispatcher:IKeuesEventDispatcher
{
  private readonly IServiceProvider _serviceProvider;
  private readonly IsSubscribedHandler _isSubscribedHandler;
  public KeuesEventDispatcher(IServiceProvider serviceProvider, IsSubscribedHandler isSubscribedHandler)
  {
    _serviceProvider = serviceProvider;
    _isSubscribedHandler = isSubscribedHandler;
  }
  public async Task DispatchAsync(IKeuesEvent keuesEvent, CancellationToken cancellationToken = default)
  {
    //Si no esta suscrito en webhooks, no se ejecuta el handler
    if(!await _isSubscribedHandler.Handle(keuesEvent.EventType))
    {
      return;
    }
    var handlerType = typeof(IKeuesEventHandler<>)
      .MakeGenericType(keuesEvent.GetType());

    dynamic handler = _serviceProvider.GetRequiredService(handlerType);

    await handler.HandleAsync((dynamic)keuesEvent, cancellationToken);
  }
}