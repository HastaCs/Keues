using Keues.Application.Features.Dashboard.GetDashboardSummary;
using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.UseCases;

public class DashboardUseCasesTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  [Fact]
  public async Task Summary_throws_when_location_not_found()
  {
    await using var context = _db.CreateContext();
    var handler = new GetDashboardSummaryHandler(context);

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new GetDashboardSummaryCommand { LocationId = Guid.NewGuid() }));
  }

  [Fact]
  public async Task Summary_counts_counters_and_queues()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    await Seed.QueueAsync(context, location.Id);
    await Seed.CounterAsync(context, location.Id);
    var handler = new GetDashboardSummaryHandler(context);

    var summary = await handler.Handle(new GetDashboardSummaryCommand
    {
      LocationId = location.Id,
      Date = DateTime.UtcNow.Date
    });

    Assert.Equal(1, summary.Counters);
    Assert.Equal(1, summary.Queues);
    Assert.Equal(location.Id, summary.Location.Id);
    Assert.Equal("Tienda Central", summary.Location.Name);
  }

  [Fact]
  public async Task Summary_aggregates_today_tickets_by_status()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var machineFlow = await Seed.FlowAsync(context, location.Id, FlowType.TicketMachine);
    var manualFlow = await Seed.FlowAsync(context, location.Id, FlowType.ManualCall);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);

    // 1 waiting, 1 in progress, 1 attended, 1 canceled, 1 manual (excluida del total).
    var waiting = await Seed.TicketAsync(context, queue.Id, machineFlow.Id);

    var inProgress = await Seed.TicketAsync(context, queue.Id, machineFlow.Id);
    inProgress.Status = TicketStatus.InProgress;
    inProgress.CounterId = counter.Id;
    inProgress.CalledAt = inProgress.CreatedAt.AddMinutes(10);
    await context.SaveChangesAsync();

    var attended = await Seed.TicketAsync(context, queue.Id, machineFlow.Id);
    attended.Status = TicketStatus.Attended;
    attended.CalledAt = attended.CreatedAt.AddMinutes(5);
    attended.AttendedAt = attended.CalledAt!.Value.AddMinutes(5);
    await context.SaveChangesAsync();

    await Seed.TicketAsync(context, queue.Id, machineFlow.Id, status: TicketStatus.Canceled);
    await Seed.TicketAsync(context, queue.Id, manualFlow.Id);

    var handler = new GetDashboardSummaryHandler(context);

    var summary = await handler.Handle(new GetDashboardSummaryCommand
    {
      LocationId = location.Id,
      Date = DateTime.UtcNow.Date
    });

    Assert.Equal(4, summary.TicketsToday.Total);
    Assert.Equal(1, summary.TicketsToday.Waiting);
    Assert.Equal(1, summary.TicketsToday.InProgress);
    Assert.Equal(1, summary.TicketsToday.Attended);
    Assert.Equal(1, summary.TicketsToday.Canceled);

    // Media de espera = (10 + 5) / 2 = 7.5; media de servicio = 5.
    Assert.Equal(7.5, summary.AverageWaitMinutes);
    Assert.Equal(5, summary.AverageServiceMinutes);

    // Ahora mismo se está atendiendo el ticket en curso.
    var nowServing = Assert.Single(summary.NowServing);
    Assert.Equal(inProgress.Id, nowServing.TicketId);
    Assert.Equal(counter.Code, nowServing.CounterCode);

    // Un ticket esperando.
    var waitingItem = Assert.Single(summary.WaitingTickets);
    Assert.Equal(waiting.Id, waitingItem.TicketId);
  }

  [Fact]
  public async Task Summary_ignores_tickets_from_other_days()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    await Seed.TicketAsync(context, queue.Id, flow.Id,
      createdAt: DateTime.UtcNow.Date.AddDays(-1).AddHours(12));
    var handler = new GetDashboardSummaryHandler(context);

    var summary = await handler.Handle(new GetDashboardSummaryCommand
    {
      LocationId = location.Id,
      Date = DateTime.UtcNow.Date
    });

    Assert.Equal(0, summary.TicketsToday.Total);
  }
}
