namespace Keues.Application.Features.Counters.AttendTicket;

public record AttendTicketCommand(Guid CounterId,Guid TicketId);