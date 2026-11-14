using Keues.API.Hubs;
using Keues.Application.Common;
using Keues.Application.Features.Qr;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Keues.API.BackgroundServices;

/// <summary>
/// Genera y rota los tokens de los flujos QR, y los envia por SignalR
/// a las TicketMachine conectadas al flujo correspondiente.
/// </summary>
public sealed class QrTokenBackgroundService : BackgroundService
{
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly IHubContext<DeviceHub> _hubContext;
  private readonly IOptions<QrOptions> _qrOptions;
  private readonly ILogger<QrTokenBackgroundService> _logger;

  public QrTokenBackgroundService(
    IServiceScopeFactory scopeFactory,
    IHubContext<DeviceHub> hubContext,
    IOptions<QrOptions> qrOptions,
    ILogger<QrTokenBackgroundService> logger)
  {
    _scopeFactory = scopeFactory;
    _hubContext = hubContext;
    _qrOptions = qrOptions;
    _logger = logger;
  }

  /// <summary>
  /// Ejecuta una rotacion inmediata al arrancar y despues espera hasta
  /// cada frontera de 5 minutos para generar y enviar el siguiente token.
  /// </summary>
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    // Primer envio para que las pantallas no esperen a la siguiente frontera.
    await RotateAsync(stoppingToken);

    while (!stoppingToken.IsCancellationRequested)
    {
      var delay = GetDelayUntilNextRotation(DateTimeOffset.UtcNow, _qrOptions.Value.SlotSeconds);

      try
      {
        await Task.Delay(delay, stoppingToken);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }

      await RotateAsync(stoppingToken);
    }
  }

  /// <summary>
  /// Calcula el tiempo que falta hasta la siguiente frontera de rotacion
  /// mas un margen de 500 ms para evitar enviar justo en el cambio de segundo.
  /// </summary>
  /// <param name="now">Instante actual en UTC.</param>
  /// <param name="slotSeconds">Duracion del periodo en segundos.</param>
  /// <returns>Tiempo de espera hasta la siguiente rotacion.</returns>
  public static TimeSpan GetDelayUntilNextRotation(
    DateTimeOffset now,
    int slotSeconds = 300)
  {
    if (slotSeconds <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(slotSeconds));
    }

    var nextSlotStart = (now.ToUnixTimeSeconds() / slotSeconds + 1) * slotSeconds;
    var target = DateTimeOffset.FromUnixTimeSeconds(nextSlotStart).AddMilliseconds(500);
    return target - now;
  }

  /// <summary>
  /// Resuelve el servicio de rotacion, construye los payloads de todos los
  /// flujos QR validos y envia cada token al grupo de TicketMachine que
  /// corresponde a su ubicacion y flujo.
  /// </summary>
  private async Task RotateAsync(CancellationToken cancellationToken)
  {
    try
    {
      using var scope = _scopeFactory.CreateScope();
      var rotationService = scope.ServiceProvider.GetRequiredService<QrTokenRotationService>();
      var now = DateTimeOffset.UtcNow;

      _logger.LogInformation("QR token rotation started at {Now}", now);
      var payloads = await rotationService.BuildPayloadsAsync(now, cancellationToken);
      _logger.LogInformation("QR token rotation generated {Count} tokens", payloads.Count);

      foreach (var payload in payloads)
      {
        _logger.LogInformation(
          "QR token generated for flow {FlowId}. RotatesAt={RotatesAt}, ExpiresAt={ExpiresAt}",
          payload.FlowId,
          payload.RotatesAt,
          payload.ExpiresAt);
        _logger.LogDebug("QR token for flow {FlowId}: {Token}", payload.FlowId, payload.Token);

        // Solo las TicketMachine conectadas a este flujo reciben el token.
        var group = DeviceGroups.TicketMachine(payload.LocationId, payload.FlowId);
        await _hubContext.Clients.Group(group).SendAsync("QrToken", payload, cancellationToken);
        _logger.LogDebug("QR token sent to group {Group}", group);
      }
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      // Shutdown in progress.
    }
    catch (Exception exception)
    {
      _logger.LogError(exception, "Failed to rotate QR tokens");
    }
  }
}
