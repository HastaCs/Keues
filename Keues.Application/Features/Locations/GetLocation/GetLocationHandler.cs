using Keues.Application.Common;
using Keues.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Locations.GetLocation;

public class GetLocationHandler
{
  private readonly IApplicationDbContext _context;
  
  public GetLocationHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  
  public async Task<LocationBaseResponse> Handle(Guid id)
  {
    var location = await _context.Locations.Include(x=>x.Counters).Include(x=>x.Queues).FirstOrDefaultAsync(l => l.Id == id);
    if (location == null)
      throw new Exception($"Location with id '{id}' not found.");
    
    return new LocationBaseResponse(location.Id, location.Name, location.Description, location.Color);
    
  }
}