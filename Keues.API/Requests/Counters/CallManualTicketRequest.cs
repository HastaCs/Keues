namespace Keues.API.Requests.Counters;

public record CallManualTicketRequest(string Code, Guid FlowId, Guid LocationId,Guid CounterId);