using Keues.Application.Features.Tickets.GetTicket;
using Keues.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Keues.Application.Events.Handlers.Tickets;

public class TicketTransferredHandler:IKeuesEventHandler<TicketTransferred>
{
  private readonly ILogger<TicketTransferredHandler> _logger;
  private readonly GetTicketHandler _getTicketHandler;
  private readonly IWebhookSender _webhookService;

  public TicketTransferredHandler(ILogger<TicketTransferredHandler> logger, GetTicketHandler getTicketHandler, IWebhookSender webhookService)
  {
    _logger = logger;
    _getTicketHandler = getTicketHandler;
    _webhookService = webhookService;
  }

  public async Task HandleAsync(TicketTransferred keuesEvent, CancellationToken cancellationToken = default)
  {
    var ticketCommand = new GetTicketCommand(keuesEvent.Id);
    var ticket = await _getTicketHandler.Handle(ticketCommand);
    _logger.LogInformation($"Ticket transferred with ID: {ticket.Id}, Code: {ticket.Code}");
    var payload=new TicketEventPayload(EventTypes.Ticket.Transferred,keuesEvent.OccurredOn,new TicketPayload(ticket.Id));
    await _webhookService.SendAsync(payload, cancellationToken);
  }
}