using Keues.Domain.Enums;

namespace Keues.Application.Features.Tickets;


public record QueueMin
{
  public Guid Id { get; set; }
  public string Name { get; set; }
}

public record CounterMin
{
  public Guid Id { get; set; }
  public string Name { get; set; }
}

public record FlowMin
{
  public Guid Id { get; set; }
  public string Name { get; set; }

}

public record GetTicketResponse
{
 public Guid Id { get; init; }
 public TicketStatus Status { get; init; }
 public DateTime CreatedAt { get; init; }
 public DateTime? CalledAt { get; init; }
 public DateTime? AttendedAt { get; init; }
 public DateTime? CanceledAt { get; init; }
 public QueueMin? Queue { get; init; }
 public CounterMin? Counter { get; init; }
 
 public required Guid FlowId { get; init; }
 public required FlowMin Flow { get; init; }

 public required Guid LocationId { get; init; }
 public string Code { get; init; }
}