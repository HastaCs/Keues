using Keues.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Keues.Application.Events.Handlers.Tickets;

public class TicketTransferredHandler : IKeuesEventHandler<TicketTransferred>
{
  private readonly ILogger<TicketTransferredHandler> _logger;
  private readonly IWebhookSender _webhookService;
  private readonly TicketEventPayloadBuilder _payloadBuilder;

  public TicketTransferredHandler(
    ILogger<TicketTransferredHandler> logger,
    IWebhookSender webhookService,
    TicketEventPayloadBuilder payloadBuilder)
  {
    _logger = logger;
    _webhookService = webhookService;
    _payloadBuilder = payloadBuilder;
  }

  public async Task HandleAsync(TicketTransferred keuesEvent, CancellationToken cancellationToken = default)
  {
    var payload = await _payloadBuilder.BuildAsync(
      EventTypes.Ticket.Transferred, keuesEvent.OccurredOn, keuesEvent.Id, keuesEvent.FromCounterId,
      keuesEvent.UserId, cancellationToken);

    _logger.LogInformation($"Ticket transferred with ID: {payload.data.id}, Code: {payload.data.code}");

    await _webhookService.SendAsync(payload, cancellationToken);
  }
}
