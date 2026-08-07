using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Locations.DeleteLocation;

public class DeleteLocationHandler
{
  private readonly IApplicationDbContext _context;
  public DeleteLocationHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  public async Task Handle(DeleteLocationCommand command)
  {
    var now = DateTime.UtcNow;
    var location = await _context.Locations.FindAsync(command.Id);
    if (location == null)
      throw new Exception($"Location with id '{command.Id}' not found.");
    
    location.RemovedAt = now;
    
    var flows=await _context.Flows.Where((x=>x.LocationId==command.Id)).ToListAsync();
    foreach (var flow in flows)
      flow.RemovedAt=now;
    
    var counters=await _context.Counters.Where((x=>x.LocationId==command.Id)).ToListAsync();
    foreach (var counter in counters)
      counter.RemovedAt=now;
    
    var queues=await _context.Queues.Where((x=>x.LocationId==command.Id)).ToListAsync();
    foreach (var queue in queues)
      queue.RemovedAt=now;
    
    
    
    await _context.SaveChangesAsync();
  }
}