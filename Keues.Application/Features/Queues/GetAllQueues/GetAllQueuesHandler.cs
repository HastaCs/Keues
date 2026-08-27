using Keues.Application.Common;
using Keues.Application.Features.Queues.GetQueue;

using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Queues.GetAllQueues;

public class GetAllQueuesHandler
{
  private readonly IApplicationDbContext _context;

  public GetAllQueuesHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<IEnumerable<QueueBaseResponse>> Handle(GetAllQueuesCommand request)
  {
    var query = _context.Queues.Include(x=>x.Counters).AsQueryable();
    if(request.LocationId.HasValue)
    {
      query = query.Where(q => q.LocationId == request.LocationId.Value);
    }
    var queues = await query.ToListAsync();
    
    
    var response= queues.Select(queue => new QueueBaseResponse(queue.Id, queue.Name, queue.Description, queue.MaxValue,
      queue.Code, queue.Priority, queue.Weight, queue.AgingIntervalMinutes,
      queue.MaxAgingBonus, queue.Color,queue.Counters.Select(x => x.Id), queue.CreatedAt));
    return response;
  }
}