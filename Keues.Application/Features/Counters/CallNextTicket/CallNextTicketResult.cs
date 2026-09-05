namespace Keues.Application.Features.Counters.CallNextTicket;

public record CallNextTicketResult(Guid TicketId, string Code,Guid QueueId);