using Keues.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Keues.Application.Events.Handlers.Tickets;

public class TicketCanceledHandler : IKeuesEventHandler<TicketCanceled>
{
  private readonly ILogger<TicketCanceledHandler> _logger;
  private readonly IWebhookSender _webhookService;
  private readonly TicketEventPayloadBuilder _payloadBuilder;

  public TicketCanceledHandler(
    ILogger<TicketCanceledHandler> logger,
    IWebhookSender webhookService,
    TicketEventPayloadBuilder payloadBuilder)
  {
    _logger = logger;
    _webhookService = webhookService;
    _payloadBuilder = payloadBuilder;
  }

  public async Task HandleAsync(TicketCanceled keuesEvent, CancellationToken cancellationToken = default)
  {
    var payload = await _payloadBuilder.BuildAsync(
      EventTypes.Ticket.Canceled, keuesEvent.OccurredOn, keuesEvent.Id, keuesEvent.CounterId, keuesEvent.UserId,
      cancellationToken);

    _logger.LogInformation($"Ticket canceled with ID: {payload.data.id}, Code: {payload.data.code}");

    await _webhookService.SendAsync(payload, cancellationToken);
  }
}
