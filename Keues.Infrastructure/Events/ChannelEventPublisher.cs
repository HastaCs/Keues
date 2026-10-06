using System.Threading.Channels;
using Keues.Application.Events;
using Keues.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Keues.Infrastructure.Events;

public class ChannelEventPublisher : IKeuesEventPublisher
{
  private readonly Channel<IKeuesEvent> _channel;
  private readonly ILogger<ChannelEventPublisher> _logger;

  public ChannelEventPublisher(Channel<IKeuesEvent> channel, ILogger<ChannelEventPublisher> logger)
  {
    _channel = channel;
    _logger = logger;
  }

  public async Task Publish<TEvent>(TEvent keuesEvent, CancellationToken cancellationToken = default) where TEvent : IKeuesEvent
  {
    _logger.LogInformation("🔥 PUBLICANDO: {EventName}", keuesEvent.GetType().Name);
    await _channel.Writer.WriteAsync(keuesEvent, cancellationToken);
  }
}
