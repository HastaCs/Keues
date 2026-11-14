using Keues.Application.Common;
using Keues.Application.DeviceRegistry.Messages;
using Keues.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Keues.Application.Features.Qr;

public sealed class QrTokenRotationService
{
  private readonly IApplicationDbContext _context;
  private readonly QrTokenPayloadFactory _payloadFactory;
  private readonly ILogger<QrTokenRotationService> _logger;

  public QrTokenRotationService(
    IApplicationDbContext context,
    QrTokenPayloadFactory payloadFactory,
    ILogger<QrTokenRotationService> logger)
  {
    _context = context;
    _payloadFactory = payloadFactory;
    _logger = logger;
  }

  /// <summary>
  /// Construye el payload de token QR de todos los flujos QrVirtual validos.
  /// Los flujos invalidos se omiten y se registran en el log.
  /// </summary>
  /// <param name="now">Instante que determina el periodo del token.</param>
  public async Task<IReadOnlyList<QrTokenPayload>> BuildPayloadsAsync(
    DateTimeOffset now,
    CancellationToken cancellationToken = default)
  {
    var flowIds = await _context.Flows
      .Where(flow => flow.FlowType == FlowType.QrVirtual)
      .Select(flow => flow.Id)
      .ToListAsync(cancellationToken);

    var payloads = new List<QrTokenPayload>();

    foreach (var flowId in flowIds)
    {
      try
      {
        var payload = await _payloadFactory.BuildAsync(flowId, now, cancellationToken);
        if (payload is not null)
        {
          payloads.Add(payload);
        }
        else
        {
          _logger.LogWarning(
            "QR flow {FlowId} was skipped because it has no valid queue",
            flowId);
        }
      }
      catch (Exception exception)
      {
        _logger.LogError(exception, "Failed to build QR token for flow {FlowId}", flowId);
      }
    }

    return payloads;
  }
}
