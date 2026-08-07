namespace Keues.Application.Features.Queues.GetAllQueues;

public record GetAllQueuesCommand
{
  public Guid? LocationId { get; set; }
}