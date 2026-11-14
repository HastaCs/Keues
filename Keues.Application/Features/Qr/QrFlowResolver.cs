using Keues.Application.Common;
using Keues.Application.Features.Flows;
using Keues.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Qr;

public sealed class QrFlowResolver
{
  private readonly IApplicationDbContext _context;

  public QrFlowResolver(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<QrFlowInfo?> TryResolveAsync(
    Guid flowId,
    CancellationToken cancellationToken = default)
  {
    var flow = await _context.Flows
      .FirstOrDefaultAsync(flow => flow.Id == flowId, cancellationToken);

    if (flow is null || flow.FlowType != FlowType.QrVirtual)
    {
      return null;
    }

    if (!QrFlowJsonParser.TryGetFirstQueueId(flow.FlowJson, out var queueId))
    {
      return null;
    }

    var queue = await _context.Queues
      .FirstOrDefaultAsync(
        queue => queue.Id == queueId && queue.LocationId == flow.LocationId,
        cancellationToken);

    return queue is null ? null : new QrFlowInfo(flow, queue);
  }

  public async Task<QrFlowInfo> ResolveAsync(
    Guid flowId,
    CancellationToken cancellationToken = default)
  {
    var result = await TryResolveAsync(flowId, cancellationToken);

    if (result is null)
    {
      throw new Exception("QR flow is not available");
    }

    return result;
  }
}
