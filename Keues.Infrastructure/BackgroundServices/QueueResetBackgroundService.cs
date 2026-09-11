using Keues.Application.Features.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Keues.Infrastructure.BackgroundServices;

public sealed class QueueResetBackgroundService : BackgroundService
{
  private readonly ILogger<QueueResetBackgroundService> _logger;
  private readonly IServiceScopeFactory _scopeFactory;

  public QueueResetBackgroundService(ILogger<QueueResetBackgroundService> logger, IServiceScopeFactory scopeFactory)
  {
    _logger = logger;
    _scopeFactory = scopeFactory;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      var now = new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute);

      _logger.LogInformation("🔥 QueueResetBackgroundService funcionando: {Time}", now);

      using var scope = _scopeFactory.CreateScope();

      var queuesUseCases = scope.ServiceProvider.GetRequiredService<QueuesUseCases>();

      await queuesUseCases.ResetQueues.Handle();

      await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
    }
  }
}