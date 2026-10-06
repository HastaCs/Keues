using Keues.Application.Features.Tickets.GetTicket;
using Keues.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Keues.Application.Events.Handlers.Tickets;

public class TicketCreatedHandler : IKeuesEventHandler<TicketCreated>
{
  private readonly ILogger<TicketCreatedHandler> _logger;
  private readonly GetTicketHandler _getTicketHandler;

  public TicketCreatedHandler(ILogger<TicketCreatedHandler> logger, GetTicketHandler getTicketHandler)
  {
    _logger = logger;
    _getTicketHandler = getTicketHandler;
  }

  public async Task HandleAsync(TicketCreated keuesEvent, CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("DISPATCHED Handling TicketCreated event.");
    var ticketCommand = new GetTicketCommand(keuesEvent.Id);
    var ticket = await _getTicketHandler.Handle(ticketCommand);
    _logger.LogInformation($"Ticket created with ID: {ticket.Id}, Codigo: {ticket.Code}");
  }
}