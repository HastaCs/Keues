using Keues.Application.Common;

namespace Keues.Application.Features.Counters.DeleteCounter;

public class DeleteCounterHandler
{
  private readonly IApplicationDbContext _context;
  public DeleteCounterHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  public async Task Handle(DeleteCounterCommand command)
  {
    var counter = await _context.Counters.FindAsync(command.Id);
    if (counter == null)
    {
      throw new Exception($"Counter with Id {command.Id} not found.");
    }
    counter.RemovedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
  }
}