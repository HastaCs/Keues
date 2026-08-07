using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Counters.GetCounter;

public class GetCounterHandler
{
  private readonly IApplicationDbContext _context;
  public GetCounterHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  public async Task<CounterBaseResponse> Handle(GetCounterCommand command)
  {
    var counter = await _context.Counters.Include(c => c.Queues).FirstOrDefaultAsync(c => c.Id == command.Id);
    if (counter == null)
    {
      throw new Exception($"Counter with Id {command.Id} not found.");
    }
    return new CounterBaseResponse(counter.Id, counter.Name, counter.Code, counter.Description, counter.Color, counter.Queues.Select(q => q.Id),counter.LocationId);
  }
}
   