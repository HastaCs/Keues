using Keues.Application.Common;
using Keues.Application.Features.Counters.GetCounter;
using Keues.Application.Features.Locations.GetLocation;
using Keues.Application.Features.Queues.GetQueue;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Locations.GetAllLocations;

public class GetAllLocationsHandler
{
  private readonly IApplicationDbContext _context;
  
  public GetAllLocationsHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  
  public async Task<IEnumerable<LocationBaseResponse>> Handle()
  { 
    var locations = await _context.Locations.Include(x=>x.Queues).Include(x=>x.Counters).ToListAsync();
    return locations.Select(location => new LocationBaseResponse(location.Id, location.Name, location.Description, location.Color));
  }
}