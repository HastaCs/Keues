namespace Keues.Application.Features.Tickets.GetAllTickets;

public record GetAllTicketsResponse(IEnumerable<GetTicketResponse> Tickets, int Page, int Limit, int Total, int TotalPages);