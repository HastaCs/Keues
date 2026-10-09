using Keues.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Keues.Application.Events.Handlers.Tickets;

public class TicketCreatedHandler : IKeuesEventHandler<TicketCreated>
{
  private readonly ILogger<TicketCreatedHandler> _logger;
  private readonly IWebhookSender _webhookService;
  private readonly TicketEventPayloadBuilder _payloadBuilder;

  public TicketCreatedHandler(
    ILogger<TicketCreatedHandler> logger,
    IWebhookSender webhookService,
    TicketEventPayloadBuilder payloadBuilder)
  {
    _logger = logger;
    _webhookService = webhookService;
    _payloadBuilder = payloadBuilder;
  }

  public async Task HandleAsync(TicketCreated keuesEvent, CancellationToken cancellationToken = default)
  {
    var payload = await _payloadBuilder.BuildAsync(
      EventTypes.Ticket.Created, keuesEvent.OccurredOn, keuesEvent.Id, null, null, cancellationToken);

    _logger.LogInformation($"Ticket created with ID: {payload.data.id}, Code: {payload.data.code}");

    await _webhookService.SendAsync(payload, cancellationToken);
  }
}
