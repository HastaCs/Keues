using Keues.Application.Common;
using Keues.Domain.Entities;

namespace Keues.Application.Features.Queues.CreateQueue;

public class CreateQueueHandler
{
  private readonly IApplicationDbContext _context;

  public CreateQueueHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<QueueBaseResponse> Handle(CreateQueueCommand command)
  {
    //TODO Mappers para estas cosas
    var queue = Queue.Create(command.Name, command.Code, command.MaxValue, command.Description, command.LocationId,
      command.Priority, command.Weight, command.AgingIntervalMinutes, command.MaxAgingBonus, command.Color);
    var counters = _context.Counters.Where(x => command.Counters.Contains(x.Id)).ToList();
    foreach (var counter in counters)
    {
      queue.Counters.Add(counter);
    }
    await _context.Queues.AddAsync(queue);
    await _context.SaveChangesAsync();
    //TODO Mapper aqui tambien
    return new QueueBaseResponse(queue.Id, queue.Name, queue.Description, queue.MaxValue, queue.Code, queue.Priority,
      queue.Weight, queue.AgingIntervalMinutes, queue.MaxAgingBonus, queue.Color, queue.Counters.Select(x => x.Id), queue.CreatedAt);
  }
}