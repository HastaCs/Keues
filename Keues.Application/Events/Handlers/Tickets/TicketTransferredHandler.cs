using Keues.Application.Features.Tickets.GetTicket;
using Keues.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Keues.Application.Events.Handlers.Tickets;

public class TicketTransferredHandler:IKeuesEventHandler<TicketTransferred>
{
  private readonly ILogger<TicketTransferredHandler> _logger;
  private readonly GetTicketHandler _getTicketHandler;

  public TicketTransferredHandler(ILogger<TicketTransferredHandler> logger, GetTicketHandler getTicketHandler)
  {
    _logger = logger;
    _getTicketHandler = getTicketHandler;
  }

  public async Task HandleAsync(TicketTransferred keuesEvent, CancellationToken cancellationToken = default)
  {
    var ticketCommand = new GetTicketCommand(keuesEvent.Id);
    var ticket = await _getTicketHandler.Handle(ticketCommand);
    _logger.LogInformation($"Ticket transferred with ID: {ticket.Id}, Code: {ticket.Code}");
  }
}