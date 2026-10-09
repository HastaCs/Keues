using Keues.Application.Features.Tickets.GetTicket;
using Keues.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Keues.Application.Events.Handlers.Tickets;

public class TicketCanceledHandler:IKeuesEventHandler<TicketCanceled> 
{
  private readonly ILogger<TicketCanceledHandler> _logger;
  private readonly GetTicketHandler _getTicketHandler;

  public TicketCanceledHandler(ILogger<TicketCanceledHandler> logger, GetTicketHandler getTicketHandler)
  {
    _logger = logger;
    _getTicketHandler = getTicketHandler;
  }

  public async Task HandleAsync(TicketCanceled keuesEvent, CancellationToken cancellationToken = default)
  {
    var ticketCommand = new GetTicketCommand(keuesEvent.Id);
    var ticket = await _getTicketHandler.Handle(ticketCommand);
    _logger.LogInformation($"Ticket canceled with ID: {ticket.Id}, Code: {ticket.Code}");
  }
}