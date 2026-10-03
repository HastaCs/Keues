using System.Diagnostics;
using System.Threading.Channels;
using Keues.Domain.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Keues.Infrastructure.Events;
public class ChannelEventConsumer : BackgroundService
{
  private readonly Channel<IKeuesEvent> _channel;
  private readonly ILogger<ChannelEventConsumer> _logger;

  public ChannelEventConsumer(Channel<IKeuesEvent> channel, ILogger<ChannelEventConsumer> logger)
  {
    _channel = channel;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    await foreach (var keuesEvent in _channel.Reader.ReadAllAsync(stoppingToken))
    {
      _logger.LogInformation($"📨 [EVENT-CONSUMER] Evento recibido: {keuesEvent.GetType().Name}");
    }
  }
}