using Keues.Application.Common;
using Keues.Application.Features.Flows.CreateFlow;
using Keues.Application.Features.Locations;
using Keues.Application.Features.Locations.GetLocation;
using Keues.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace Keues.Application.Features.Flows.CreateFlow;

public class CreateFlowHandler
{
  private readonly IApplicationDbContext _context;
  public CreateFlowHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<FlowBaseResponse> Handle(CreateFlowCommand command)
  {
    var location=await _context.Locations.Include(x=>x.Counters).Include(x=>x.Queues).FirstOrDefaultAsync(l => l.Id == command.LocationId); 
    if(location == null)
      throw new Exception($"Location with id {command.LocationId} not found");
    var flow = new Flow()
    {
      Name = command.Name,
      FlowType = command.FlowType,
      Description = command.Description,
      LocationId = command.LocationId,
      FlowJson = command.FlowJson
      
    };
    await _context.Flows.AddAsync(flow);
    await _context.SaveChangesAsync();
    
    
    //Hay que attachar el location al flow
    
    return new FlowBaseResponse(flow.Id,flow.Name,flow.Description,flow.FlowType,flow.FlowJson,new LocationBaseResponse(location.Id,location.Name,location.Description, location.Color),flow.CreatedAt);
  }
}