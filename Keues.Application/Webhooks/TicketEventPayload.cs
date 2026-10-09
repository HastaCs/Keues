namespace Keues.Application.Events;

public record TicketEventPayload(string eventType, DateTime occurredAt, TicketEventData data);

public record TicketEventData(
  Guid id,
  string code,
  EntityRef queue,
  EntityRef? counter,
  EntityRef? user,
  EntityRef location);

public record EntityRef(Guid id, string name);
