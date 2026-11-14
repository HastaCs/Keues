using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Flows;

public static class QrFlowQueueValidator
{
  public static async Task ValidateAsync(
    IApplicationDbContext context,
    Guid locationId,
    string flowJson,
    CancellationToken cancellationToken = default)
  {
    if (!QrFlowJsonParser.TryGetFirstQueueId(flowJson, out var queueId))
    {
      throw new Exception("QR flows require a queue in FlowJson");
    }

    var queueExists = await context.Queues
      .AnyAsync(queue => queue.Id == queueId && queue.LocationId == locationId, cancellationToken);

    if (!queueExists)
    {
      throw new Exception($"Queue with id {queueId} not found in location {locationId}");
    }
  }
}
