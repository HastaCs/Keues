namespace Keues.Application.Features.Counters.TransferTicket;

public record TransferTicketCommand(Guid CounterId, Guid TicketId, Guid QueueId) ;