using Keues.Application.Features.Tickets.GetAllTickets;
using Keues.Application.Features.Tickets.GetTicket;
using Keues.Application.Features.Tickets.GetTicketHistory;

namespace Keues.Application.Features.Tickets;

public class TicketsUseCases(GetAllTicketsHandler getAllTickets, GetTicketHandler getTicket, GetTicketHistoryHandler getTicketHistory)
{
  public GetAllTicketsHandler GetAllTickets => getAllTickets;
  public GetTicketHandler GetTicket => getTicket;
  public GetTicketHistoryHandler GetTicketHistory => getTicketHistory;
}