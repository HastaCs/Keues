using Keues.Application.Features.Tickets.GetTicket;
using Keues.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Keues.Application.Events.Handlers.Tickets;

public class TicketCalledHandler:IKeuesEventHandler<TicketCalled>
{
  private readonly ILogger<TicketCalledHandler> _logger;
  private readonly GetTicketHandler _getTicketHandler;
  private readonly IWebhookSender _webhookService;

  public TicketCalledHandler(ILogger<TicketCalledHandler> logger, GetTicketHandler getTicketHandler, IWebhookSender webhookService)
  {
    _logger = logger;
    _getTicketHandler = getTicketHandler;
    _webhookService = webhookService;
  }

  public async Task HandleAsync(TicketCalled keuesEvent, CancellationToken cancellationToken = default)
  {
    var ticketCommand = new GetTicketCommand(keuesEvent.Id);
    var ticket = await _getTicketHandler.Handle(ticketCommand);
    _logger.LogInformation($"Ticket called with ID: {ticket.Id}, Code: {ticket.Code}");
    var payload=new TicketEventPayload(EventTypes.Ticket.Called,keuesEvent.OccurredOn,new TicketPayload(ticket.Id));
    await _webhookService.SendAsync(payload, cancellationToken);
  }
}