using Keues.Application.Features.Tickets.GetAllTickets;
using Keues.Application.Features.Tickets.GetTicket;

namespace Keues.Application.Features.Tickets;

public class TicketsUseCases(GetAllTicketsHandler getAllTickets, GetTicketHandler getTicket)
{
  public GetAllTicketsHandler GetAllTickets => getAllTickets;
  public GetTicketHandler GetTicket => getTicket;
  }