namespace Keues.Application.Features.Dashboard.GetDashboardSummary;

public record LocationSummary
{
  public Guid Id { get; init; }
  public string Name { get; init; }
  public string? Description { get; init; }
  public string Color { get; init; }
}

public record TicketsTodaySummary
{
  public int Total { get; init; }
  public int Waiting { get; init; }
  public int InProgress { get; init; }
  public int Attended { get; init; }
  public int Canceled { get; init; }
}

public record NowServingItem
{
  public Guid CounterId { get; init; }
  public string CounterName { get; init; }
  public string CounterCode { get; init; }
  public string CounterColor { get; init; }
  public Guid TicketId { get; init; }
  public string TicketCode { get; init; }
  public Guid QueueId { get; init; }
  public string QueueName { get; init; }
  public DateTime? CalledAt { get; init; }
}

public record WaitingTicketItem
{
  public Guid TicketId { get; init; }
  public string TicketCode { get; init; }
  public Guid QueueId { get; init; }
  public string QueueName { get; init; }
  public string QueueColor { get; init; }
  public DateTime CreatedAt { get; init; }
  public int WaitingMinutes { get; init; }
}

public record GetDashboardSummaryResponse
{
  public LocationSummary Location { get; init; }
  public int Counters { get; init; }
  public int Queues { get; init; }
  public TicketsTodaySummary TicketsToday { get; init; }
  public double? AverageWaitMinutes { get; init; }
  public double? AverageServiceMinutes { get; init; }
  public IEnumerable<NowServingItem> NowServing { get; init; } = [];
  public IEnumerable<WaitingTicketItem> WaitingTickets { get; init; } = [];
}
