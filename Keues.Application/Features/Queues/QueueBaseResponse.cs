namespace Keues.Application.Features.Queues;

public record QueueBaseResponse(Guid Id, string Name, string Description,int? MaxValue,string Code,int Priority,int Weight,int AgingIntervalMinutes,int MaxAgingBonus,string Color,IEnumerable<Guid> Counters);