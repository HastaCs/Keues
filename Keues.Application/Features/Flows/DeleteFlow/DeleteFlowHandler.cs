using Keues.Application.Common;


namespace Keues.Application.Features.Flows.DeleteFlow;

public class DeleteFlowHandler
{
  private readonly IApplicationDbContext _context;
  public DeleteFlowHandler(IApplicationDbContext context)
  {
    _context = context;
  }
    
  public async Task Handle(DeleteFlowCommand command)
  {
    var queueSystem = await _context.Flows.FindAsync(command.Id);
    if (queueSystem == null)
      throw new Exception($"Queue system with ID {command.Id} not found.");
    
    queueSystem.RemovedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
  }
}