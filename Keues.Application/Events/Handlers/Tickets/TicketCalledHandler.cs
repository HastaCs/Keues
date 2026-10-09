using Keues.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Keues.Application.Events.Handlers.Tickets;

public class TicketCalledHandler : IKeuesEventHandler<TicketCalled>
{
  private readonly ILogger<TicketCalledHandler> _logger;
  private readonly IWebhookSender _webhookService;
  private readonly TicketEventPayloadBuilder _payloadBuilder;

  public TicketCalledHandler(
    ILogger<TicketCalledHandler> logger,
    IWebhookSender webhookService,
    TicketEventPayloadBuilder payloadBuilder)
  {
    _logger = logger;
    _webhookService = webhookService;
    _payloadBuilder = payloadBuilder;
  }

  public async Task HandleAsync(TicketCalled keuesEvent, CancellationToken cancellationToken = default)
  {
    var payload = await _payloadBuilder.BuildAsync(
      EventTypes.Ticket.Called, keuesEvent.OccurredOn, keuesEvent.Id, keuesEvent.CounterId, keuesEvent.UserId,
      cancellationToken);

    _logger.LogInformation($"Ticket called with ID: {payload.data.id}, Code: {payload.data.code}");

    await _webhookService.SendAsync(payload, cancellationToken);
  }
}
