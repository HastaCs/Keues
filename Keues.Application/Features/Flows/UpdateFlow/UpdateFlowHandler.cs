using Keues.Application.Common;


namespace Keues.Application.Features.Flows.UpdateFlow;

public class UpdateFlowHandler
{
  private readonly IApplicationDbContext _context;
  public UpdateFlowHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<UpdateFlowResponse> Handle(UpdateFlowCommand command)
  {
    var flow = await _context.Flows.FindAsync(command.Id);
    if (flow == null)
    {
     throw new Exception($"Queue system with ID {command.Id} not found.");
    }

    flow.Name = command.Name;
    flow.Description = command.Description;
    flow.FlowJson = command.FlowJson;

    await _context.SaveChangesAsync();

    return new UpdateFlowResponse(flow.Id, flow.Name, flow.Description);
  }
}