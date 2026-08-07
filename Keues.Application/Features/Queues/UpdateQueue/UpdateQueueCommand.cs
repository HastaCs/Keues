namespace Keues.Application.Features.Queues.UpdateQueue;

public record UpdateQueueCommand:QueueBaseCommand
{
  public Guid Id { get; set; }
}
