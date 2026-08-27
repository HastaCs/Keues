using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Queues.GetQueue;

public class GetQueueHandler
{
  private readonly IApplicationDbContext _context;

  public GetQueueHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<QueueBaseResponse> Handle(GetQueueCommand command)
  {
    var queue = await _context.Queues.Include(x => x.Counters).FirstOrDefaultAsync(q => q.Id == command.Id);
    if (queue == null)
      throw new Exception($"Ticket type with id '{command.Id}' not found.");

    return new QueueBaseResponse(queue.Id, queue.Name, queue.Description, queue.MaxValue,
      queue.Code, queue.Priority, queue.Weight, queue.AgingIntervalMinutes,
      queue.MaxAgingBonus, queue.Color,queue.Counters.Select(x => x.Id), queue.CreatedAt);
  }
}