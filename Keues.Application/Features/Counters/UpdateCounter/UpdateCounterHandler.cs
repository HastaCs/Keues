using Keues.Application.Common;
using Keues.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Counters.UpdateCounter;

public class UpdateCounterHandler
{
  private readonly IApplicationDbContext _context;

  public UpdateCounterHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<CounterBaseResponse> Handle(UpdateCounterCommand command)
  {
    var counter = await _context.Counters
      .Include(c => c.Queues)
      .FirstOrDefaultAsync(c => c.Id == command.Id);
    if (counter == null)
    {
      throw new Exception($"Counter with Id {command.Id} not found.");
    }

    counter.Name = command.Name;
    counter.Code = command.Code;
    counter.Description = command.Description;
    counter.LocationId = command.LocationId;
    counter.Color = command.Color;
    //  counter.Queues.Clear();
    var toRemove = counter.Queues.ToList();

    foreach (var queue in toRemove)
    {
      counter.Queues.Remove(queue);
    }

    // await _context.SaveChangesAsync();
    if (command.Queues != null)
    {
      var queues = await _context.Queues
        .Where(q => command.Queues.Contains(q.Id))
        .ToListAsync();
      foreach (var queue in queues)
      {
        counter.Queues.Add(queue);
      }
    }

    await _context.SaveChangesAsync();
    return new CounterBaseResponse(counter.Id, counter.Name, counter.Code, counter.Description, counter.Color, counter.Queues.Select(q => q.Id),counter.LocationId);
  }
}