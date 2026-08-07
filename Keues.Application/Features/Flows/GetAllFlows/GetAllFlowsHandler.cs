using Keues.Application.Common;
using Keues.Application.Features.Flows.GetFlow;
using Keues.Application.Features.Locations;
using Keues.Application.Features.Locations.GetLocation;

using Keues.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Flows.GetAllFlows;

public class GetAllFlowsHandler
{
  private readonly IApplicationDbContext  _context;

  public GetAllFlowsHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  
  public async Task<IEnumerable<FlowBaseResponse>> Handle(GetAllFlowsCommand command)
  {
    var query = _context.Flows.Include(f => f.Location).AsQueryable();

    if (command.LocationId.HasValue)
    {
      query = query.Where(f => f.LocationId == command.LocationId.Value);
    }
//TODO Reviar esta response, no creo que haga falta el numero de counters ni queues
    var flows = await query.ToListAsync();
    return flows.Select(x => new FlowBaseResponse(x.Id, x.Name, x.Description,x.FlowType,x.FlowJson,new LocationBaseResponse(x.Location.Id,x.Location.Name,x.Location.Description, x.Location.Color),x.CreatedAt));
  }
  
  
}