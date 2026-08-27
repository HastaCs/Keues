namespace Keues.Application.Features.Counters;

public record CounterBaseResponse(Guid Id, string Name, string Code, string? Description, string? Color,IEnumerable<Guid> Queues,Guid LocationId, DateTime CreatedAt);