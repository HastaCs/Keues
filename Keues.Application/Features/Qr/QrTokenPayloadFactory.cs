using Keues.Application.Common;
using Keues.Application.DeviceRegistry.Messages;
using Microsoft.Extensions.Options;

namespace Keues.Application.Features.Qr;

/// <summary>
/// Construye el mensaje QrTokenPayload que se envia a una TicketMachine.
/// Resuelve el flujo QR, genera el token del periodo actual y calcula la URL
/// publica que debe codificar el QR.
/// </summary>
public sealed class QrTokenPayloadFactory
{
  private readonly QrFlowResolver _resolver;
  private readonly IQrTokenService _tokenService;
  private readonly QrOptions _options;

  public QrTokenPayloadFactory(
    QrFlowResolver resolver,
    IQrTokenService tokenService,
    IOptions<QrOptions> options)
  {
    _resolver = resolver;
    _tokenService = tokenService;
    _options = options.Value;
  }

  /// <summary>
  /// Genera el payload de un flujo QR para el instante indicado.
  /// Devuelve null si el flujo no es QR o no tiene una cola valida.
  /// </summary>
  /// <param name="flowId">Identificador del flujo QR.</param>
  /// <param name="now">Instante que determina el periodo del token.</param>
  /// <returns>Payload listo para enviar por SignalR, o null si el flujo no es valido.</returns>
  public async Task<QrTokenPayload?> BuildAsync(
    Guid flowId,
    DateTimeOffset now,
    CancellationToken cancellationToken = default)
  {
    // Valida que el flujo sea QrVirtual y que su FlowJson apunte a una cola real.
    var qrFlow = await _resolver.TryResolveAsync(flowId, cancellationToken);

    if (qrFlow is null)
    {
      return null;
    }

    // Periodo actual de 5 minutos: Start/End y duracion del bloque.
    var period = _tokenService.GetPeriod(now);

    // Token opaco del flujo para este periodo.
    var token = _tokenService.Create(flowId, now);

    // URL que la TicketMachine codificara dentro del QR.
    var url = BuildUrl(flowId, token);

    return new QrTokenPayload(
      qrFlow.Flow.Id,
      qrFlow.Flow.LocationId,
      token,
      // Momento en el que la pantalla debe rotar al siguiente token.
      period.End,
      // El token sigue siendo valido durante el periodo siguiente.
      period.End.Add(period.Duration),
      url);
  }

  /// <summary>
  /// Construye la URL publica del QR. Si no hay PublicBaseUrl configurada,
  /// devuelve una ruta relativa para que la resuelva la pantalla.
  /// </summary>
  /// <param name="flowId">Identificador del flujo QR.</param>
  /// <param name="token">Token del periodo actual.</param>
  private string BuildUrl(Guid flowId, string token)
  {
    var path = $"/turno/{flowId}/{token}";
    var baseUrl = _options.PublicBaseUrl.TrimEnd('/');

    return string.IsNullOrWhiteSpace(baseUrl) ? path : $"{baseUrl}{path}";
  }
}
