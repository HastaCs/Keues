using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Counters.GetQueues;

public class GetQueuesHandle
{
  private readonly IApplicationDbContext _context;
  
  public GetQueuesHandle(IApplicationDbContext context)
  {
    _context = context;
  }
  public async Task<IEnumerable<GetQueuesResponse>> Handle(GetQueuesQuery query)
  {
    var counter = await _context.Counters.FirstOrDefaultAsync(c => c.Id == query.CounterId);
    if (counter == null)
    {
      throw new Exception($"Counter with Id {query.CounterId} not found.");
    }
   
    var queues=await _context.Queues.Where(q => q.Counters.Any(c=>c.Id==query.CounterId))
            .Select(s=>new GetQueuesResponse(s.Id,s.Code)).ToListAsync();
    return queues;
  }
}