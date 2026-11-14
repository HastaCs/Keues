namespace Keues.API.Requests.Queues;

public record CreateQrTicketRequest(Guid FlowId, string Token);
