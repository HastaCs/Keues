using Keues.Application.Common;
using Keues.Application.Features.Locations;
using Keues.Application.Features.Locations.GetLocation;

using Keues.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Flows.GetFlow;

public class GetFlowHandler
{
  private readonly IApplicationDbContext _context;
  public GetFlowHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  public async Task<FlowBaseResponse> Handle(GetFlowCommand command)
  {
    var flow = await _context.Flows.Include(q => q.Location).FirstOrDefaultAsync(q => q.Id == command.Id);
    if (flow == null)
      throw new Exception($"Queue system with ID {command.Id} not found.");
        
    var location=new LocationBaseResponse(flow.Location.Id,flow.Location.Name,flow.Location.Description, flow.Location.Color);
    return new  FlowBaseResponse(flow.Id, flow.Name, flow.Description,flow.FlowType,flow.FlowJson,location,flow.CreatedAt);
  }
}