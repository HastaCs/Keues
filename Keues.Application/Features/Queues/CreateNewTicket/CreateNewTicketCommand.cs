namespace Keues.Application.Features.Queues.CreateNewTicket;

public record CreateNewTicketCommand(Guid QueueId,Guid FlowId);