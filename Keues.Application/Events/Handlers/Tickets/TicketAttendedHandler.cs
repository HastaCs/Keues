using Keues.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Keues.Application.Events.Handlers.Tickets;

public class TicketAttendedHandler : IKeuesEventHandler<TicketAttended>
{
  private readonly ILogger<TicketAttendedHandler> _logger;
  private readonly IWebhookSender _webhookService;
  private readonly TicketEventPayloadBuilder _payloadBuilder;

  public TicketAttendedHandler(
    ILogger<TicketAttendedHandler> logger,
    IWebhookSender webhookService,
    TicketEventPayloadBuilder payloadBuilder)
  {
    _logger = logger;
    _webhookService = webhookService;
    _payloadBuilder = payloadBuilder;
  }

  public async Task HandleAsync(TicketAttended keuesEvent, CancellationToken cancellationToken = default)
  {
    var payload = await _payloadBuilder.BuildAsync(
      EventTypes.Ticket.Attended, keuesEvent.OccurredOn, keuesEvent.Id, keuesEvent.CounterId, keuesEvent.UserId,
      cancellationToken);

    _logger.LogInformation($"Ticket attended with ID: {payload.data.id}, Code: {payload.data.code}");

    await _webhookService.SendAsync(payload, cancellationToken);
  }
}
