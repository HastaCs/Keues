using Keues.Application.Common;

namespace Keues.Application.Features.Queues.DeleteQueue;

public class DeleteQueueHandler(IApplicationDbContext context)
{
  public async Task Handle(DeleteQueueCommand command)
  {
    var queue = await context.Queues.FindAsync(command.Id);
    if (queue == null)
    {
      throw new Exception("Ticket type not found");
    }

    queue.RemovedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();
  }
}
