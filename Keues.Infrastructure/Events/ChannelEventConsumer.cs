using System.Diagnostics;
using System.Threading.Channels;
using Keues.Application.Events;
using Keues.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Keues.Infrastructure.Events;

public class ChannelEventConsumer : BackgroundService
{
  private readonly Channel<IKeuesEvent> _channel;
  private readonly ILogger<ChannelEventConsumer> _logger;
  private readonly IServiceScopeFactory _scopeFactory;

  public ChannelEventConsumer(Channel<IKeuesEvent> channel, ILogger<ChannelEventConsumer> logger,
    IServiceScopeFactory scopeFactory)
  {
    _channel = channel;
    _logger = logger;
    _scopeFactory = scopeFactory;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    await foreach (var keuesEvent in _channel.Reader.ReadAllAsync(stoppingToken))
    {
      _logger.LogInformation($"📨 [EVENT-CONSUMER] Evento recibido: {keuesEvent.GetType().Name}");
      using var scope = _scopeFactory.CreateScope();

      var dispatcher = scope.ServiceProvider.GetRequiredService<IKeuesEventDispatcher>();

      await dispatcher.DispatchAsync(keuesEvent, stoppingToken);
    }
  }
}