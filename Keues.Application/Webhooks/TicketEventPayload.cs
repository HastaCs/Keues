namespace Keues.Application.Events;

public record TicketEventPayload(string eventType, DateTime OccurredAt, TicketPayload Payload);

public record TicketPayload(Guid Id);
