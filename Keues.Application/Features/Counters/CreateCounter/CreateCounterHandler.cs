using Keues.Application.Common;
using Keues.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Counters.CreateCounter;

public class CreateCounterHandler(IApplicationDbContext _context)
{
  public async Task<CounterBaseResponse> Handle(CreateCounterCommand command)
  {
    var counter = Counter.Create(command.Name, command.Description, command.Code, command.Color,command.LocationId);
    if (command.Queues != null )
    {
      var queues = await _context.Queues
        .Where(q => command.Queues.Contains(q.Id))
        .ToListAsync();
      foreach (var queue in queues)
      {
        counter.Queues.Add(queue);
      }
    }
    _context.Counters.Add(counter);
    await _context.SaveChangesAsync();

    return new CounterBaseResponse(counter.Id, counter.Name, counter.Code, counter.Description, counter.Color,counter.Queues.Select(q => q.Id),counter.LocationId,counter.CreatedAt!.Value);
   
  }
}