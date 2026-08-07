using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Queues.UpdateQueue;

public class UpdateQueueHandler
{
  private readonly IApplicationDbContext _context;
  public UpdateQueueHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<QueueBaseResponse> Handle(UpdateQueueCommand command)
  {
    var queue=await _context.Queues.Include(q => q.Counters).FirstOrDefaultAsync(q => q.Id == command.Id);
    if(queue==null)
      throw new Exception($"Queue with id '{command.Id}' not found.");
    
    queue.Code=command.Code;
    queue.Description=command.Description;
    queue.MaxValue=command.MaxValue;
    queue.Name=command.Name;
    queue.LocationId=command.LocationId;
    queue.Color=command.Color;
    queue.MaxAgingBonus=command.MaxAgingBonus;
    queue.Priority=command.Priority;
    queue.AgingIntervalMinutes=command.AgingIntervalMinutes;
    queue.Weight=command.Weight;
    queue.Counters.Clear();
    if (command.Counters != null)
    {
      var counters = await _context.Counters.Where(c => command.Counters.Contains(c.Id)).ToListAsync();
      foreach (var counter in counters)
        queue.Counters.Add(counter);
    }

    await _context.SaveChangesAsync();
   
    return  new QueueBaseResponse(queue.Id, queue.Name, queue.Description, queue.MaxValue, queue.Code, queue.Priority, queue.Weight, queue.AgingIntervalMinutes, queue.MaxAgingBonus, queue.Color, queue.Counters.Select(x => x.Id));
  }
  
}