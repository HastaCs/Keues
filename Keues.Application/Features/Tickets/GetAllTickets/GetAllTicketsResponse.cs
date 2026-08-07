namespace Keues.Application.Features.Tickets.GetAllTickets;

public record GetAllTicketsResponse(IEnumerable<GetTicketResponse> Tickets);