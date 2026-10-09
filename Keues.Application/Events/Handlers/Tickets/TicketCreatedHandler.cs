using Keues.Application.Features.Tickets.GetTicket;
using Keues.Application.Features.WebhooksConfig.IsSubscribed;
using Keues.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Keues.Application.Events.Handlers.Tickets;

public class TicketCreatedHandler : IKeuesEventHandler<TicketCreated>
{
  private readonly ILogger<TicketCreatedHandler> _logger;
  private readonly GetTicketHandler _getTicketHandler;
private readonly IWebhookSender _webhookService;
  public TicketCreatedHandler(ILogger<TicketCreatedHandler> logger, GetTicketHandler getTicketHandler, IWebhookSender webhookService)
  {
    _logger = logger;
    _getTicketHandler = getTicketHandler;
    _webhookService = webhookService;
  }

  public async Task HandleAsync(TicketCreated keuesEvent, CancellationToken cancellationToken = default)
  {
   var ticketCommand = new GetTicketCommand(keuesEvent.Id);
    var ticket = await _getTicketHandler.Handle(ticketCommand);
    _logger.LogInformation($"Ticket created with ID: {ticket.Id}, Code: {ticket.Code}");
    var payload=new TicketEventPayload(EventTypes.Ticket.Created,keuesEvent.OccurredOn,new TicketPayload(ticket.Id));
    await _webhookService.SendAsync(payload, cancellationToken);
  }
}