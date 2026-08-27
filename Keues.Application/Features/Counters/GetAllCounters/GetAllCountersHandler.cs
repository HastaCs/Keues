using Keues.Application.Common;
using Keues.Application.Features.Counters.GetCounter;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Counters.GetAllCounters;

public class GetAllCountersHandler
{
  private readonly IApplicationDbContext _context;
  public GetAllCountersHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  public async Task<IEnumerable<CounterBaseResponse>> Handle(GetAllCountersCommand command)
  {
    var query=_context.Counters.Include(x=>x.Queues).AsQueryable();
    if(command.LocationId.HasValue)
      query=query.Where(x=>x.LocationId==command.LocationId.Value);
    
    var counters = await query.ToListAsync();
    return counters.Select(counter => new CounterBaseResponse(counter.Id, counter.Name, counter.Code, counter.Description, counter.Color, counter.Queues.Select(x => x.Id),counter.LocationId,counter.CreatedAt!.Value));
  }
}