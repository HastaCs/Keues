using Keues.Application.Features.Tickets.GetTicket;
using Keues.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Keues.Application.Events.Handlers.Tickets;

public class TicketCalledHandler:IKeuesEventHandler<TicketCalled>
{
  private readonly ILogger<TicketCalledHandler> _logger;
  private readonly GetTicketHandler _getTicketHandler;

  public TicketCalledHandler(ILogger<TicketCalledHandler> logger, GetTicketHandler getTicketHandler)
  {
    _logger = logger;
    _getTicketHandler = getTicketHandler;
  }

  public async Task HandleAsync(TicketCalled keuesEvent, CancellationToken cancellationToken = default)
  {
    var ticketCommand = new GetTicketCommand(keuesEvent.Id);
    var ticket = await _getTicketHandler.Handle(ticketCommand);
    _logger.LogInformation($"Ticket called with ID: {ticket.Id}, Code: {ticket.Code}");
  }
}