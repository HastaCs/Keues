using Keues.Application.Common;
using Keues.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Dashboard.GetDashboardSummary;

public class GetDashboardSummaryHandler
{
  private readonly IApplicationDbContext _context;

  public GetDashboardSummaryHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<GetDashboardSummaryResponse> Handle(GetDashboardSummaryCommand command)
  {
    var location = await _context.Locations.FirstOrDefaultAsync(x => x.Id == command.LocationId);
    if (location == null)
      throw new Exception("Location not found");

    var day = command.Date?.Date ?? DateTime.UtcNow.Date;
    var start = day;
    var end = day.AddDays(1);

    var counters = await _context.Counters.CountAsync(x => x.LocationId == command.LocationId);
    var queues = await _context.Queues.CountAsync(x => x.LocationId == command.LocationId);

    var tickets = await _context.Tickets
      .Include(x => x.Queue)
      .Include(x => x.Counter)
      .Include(x => x.Flow)
      .Where(x =>
        x.Queue.LocationId == command.LocationId &&
        x.Flow.FlowType == FlowType.TicketMachine &&
        x.CreatedAt >= start &&
        x.CreatedAt < end)
      .ToListAsync();

    var ticketsToday = new TicketsTodaySummary
    {
      Total = tickets.Count,
      Waiting = tickets.Count(x => x.Status == TicketStatus.Waiting),
      InProgress = tickets.Count(x => x.Status == TicketStatus.InProgress),
      Attended = tickets.Count(x => x.Status == TicketStatus.Attended),
      Canceled = tickets.Count(x => x.Status == TicketStatus.Canceled)
    };

    var calledTickets = tickets.Where(x => x.CalledAt.HasValue).ToList();
    double? averageWaitMinutes = calledTickets.Count > 0
      ? calledTickets.Average(x => (x.CalledAt!.Value - x.CreatedAt).TotalMinutes)
      : null;

    var attendedTickets = tickets.Where(x => x.AttendedAt.HasValue && x.CalledAt.HasValue).ToList();
    double? averageServiceMinutes = attendedTickets.Count > 0
      ? attendedTickets.Average(x => (x.AttendedAt!.Value - x.CalledAt!.Value).TotalMinutes)
      : null;

    var nowServing = tickets
      .Where(x => x.Status == TicketStatus.InProgress && x.Counter != null)
      .OrderBy(x => x.CalledAt)
      .Select(x => new NowServingItem
      {
        CounterId = x.Counter!.Id,
        CounterName = x.Counter.Name,
        CounterCode = x.Counter.Code,
        CounterColor = x.Counter.Color,
        TicketId = x.Id,
        TicketCode = x.Code,
        QueueId = x.Queue.Id,
        QueueName = x.Queue.Name,
        CalledAt = x.CalledAt
      })
      .ToList();

    var waitingTickets = tickets
      .Where(x => x.Status == TicketStatus.Waiting)
      .OrderBy(x => x.CreatedAt)
      .Select(x => new WaitingTicketItem
      {
        TicketId = x.Id,
        TicketCode = x.Code,
        QueueId = x.Queue.Id,
        QueueName = x.Queue.Name,
        QueueColor = x.Queue.Color,
        CreatedAt = x.CreatedAt,
        WaitingMinutes = (int)Math.Max(0, (DateTime.UtcNow - x.CreatedAt).TotalMinutes)
      })
      .ToList();

    return new GetDashboardSummaryResponse
    {
      Location = new LocationSummary
      {
        Id = location.Id,
        Name = location.Name,
        Description = location.Description,
        Color = location.Color
      },
      Counters = counters,
      Queues = queues,
      TicketsToday = ticketsToday,
      AverageWaitMinutes = averageWaitMinutes,
      AverageServiceMinutes = averageServiceMinutes,
      NowServing = nowServing,
      WaitingTickets = waitingTickets
    };
  }
}
