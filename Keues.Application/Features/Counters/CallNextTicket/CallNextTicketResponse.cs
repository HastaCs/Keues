namespace Keues.Application.Features.Counters.CallNextTicket;

public record CallNextTicketResponse(Guid TicketId, string Code,Guid QueueId);