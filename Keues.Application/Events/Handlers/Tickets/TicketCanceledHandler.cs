using Keues.Application.Features.Tickets.GetTicket;
using Keues.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Keues.Application.Events.Handlers.Tickets;

public class TicketCanceledHandler:IKeuesEventHandler<TicketCanceled> 
{
  private readonly ILogger<TicketCanceledHandler> _logger;
  private readonly GetTicketHandler _getTicketHandler;
  private readonly IWebhookSender _webhookService;

  public TicketCanceledHandler(ILogger<TicketCanceledHandler> logger, GetTicketHandler getTicketHandler, IWebhookSender webhookService)
  {
    _logger = logger;
    _getTicketHandler = getTicketHandler;
    _webhookService = webhookService;
  }

  public async Task HandleAsync(TicketCanceled keuesEvent, CancellationToken cancellationToken = default)
  {
    var ticketCommand = new GetTicketCommand(keuesEvent.Id);
    var ticket = await _getTicketHandler.Handle(ticketCommand);
    _logger.LogInformation($"Ticket canceled with ID: {ticket.Id}, Code: {ticket.Code}");
    var payload=new TicketEventPayload(EventTypes.Ticket.Canceled,keuesEvent.OccurredOn,new TicketPayload(ticket.Id));
    await _webhookService.SendAsync(payload, cancellationToken);
  }
}