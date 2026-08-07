using Keues.Application.Features.Counters;
using Keues.Application.Features.Counters.AttendTicket;
using Keues.Application.Features.Counters.CallNextTicket;
using Keues.Application.Features.Counters.CancelTicket;
using Keues.Application.Features.Counters.CreateCounter;
using Keues.Application.Features.Counters.DeleteCounter;
using Keues.Application.Features.Counters.GetAllCounters;
using Keues.Application.Features.Counters.GetCounter;
using Keues.Application.Features.Counters.UpdateCounter;
using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.UseCases;

public class CounterUseCasesTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  [Fact]
  public async Task Create_persists_and_links_the_queues()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var q1 = await Seed.QueueAsync(context, location.Id, code: "A");
    var q2 = await Seed.QueueAsync(context, location.Id, code: "B");
    var handler = new CreateCounterHandler(context);

    var response = await handler.Handle(new CreateCounterCommand
    {
      Name = "Caja 1",
      Code = "C1",
      Color = "green",
      Description = "Caja principal",
      LocationId = location.Id,
      Queues = [q1.Id, q2.Id]
    });

    Assert.NotEqual(Guid.Empty, response.Id);
    Assert.Equal("Caja 1", response.Name);
    Assert.Equal(2, response.Queues.Count());
    Assert.Contains(q1.Id, response.Queues);
    Assert.Contains(q2.Id, response.Queues);
  }

  [Fact]
  public async Task Get_throws_when_not_found()
  {
    await using var context = _db.CreateContext();
    var handler = new GetCounterHandler(context);

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new GetCounterCommand(Guid.NewGuid())));
  }

  [Fact]
  public async Task GetAll_filters_by_location()
  {
    await using var context = _db.CreateContext();
    var locA = await Seed.LocationAsync(context, "A");
    var locB = await Seed.LocationAsync(context, "B");
    await Seed.CounterAsync(context, locA.Id, code: "A1");
    await Seed.CounterAsync(context, locA.Id, code: "A2");
    await Seed.CounterAsync(context, locB.Id, code: "B1");
    var handler = new GetAllCountersHandler(context);

    var onlyA = await handler.Handle(new GetAllCountersCommand { LocationId = locA.Id });

    Assert.Equal(2, onlyA.Count());
  }

  [Fact]
  public async Task Update_replaces_the_linked_queues()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var q1 = await Seed.QueueAsync(context, location.Id, code: "A");
    var q2 = await Seed.QueueAsync(context, location.Id, code: "B");
    var counter = await Seed.CounterAsync(context, location.Id, queues: [q1]);
    var handler = new UpdateCounterHandler(context);

    var response = await handler.Handle(new UpdateCounterCommand
    {
      Id = counter.Id,
      Name = "Caja nueva",
      Code = "C2",
      Color = "yellow",
      Description = "Caja 2",
      LocationId = location.Id,
      Queues = [q2.Id]
    });

    Assert.Equal("Caja nueva", response.Name);
    Assert.Equal([q2.Id], response.Queues);
  }

  [Fact]
  public async Task Delete_soft_deletes_the_counter()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var counter = await Seed.CounterAsync(context, location.Id);
    var handler = new DeleteCounterHandler(context);

    await handler.Handle(new DeleteCounterCommand(counter.Id));

    var getAll = await new GetAllCountersHandler(context).Handle(new GetAllCountersCommand());
    Assert.Empty(getAll);
  }

  [Fact]
  public async Task CallNextTicket_returns_null_when_there_are_no_waiting_tickets()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    var handler = new CallNextTicketHandler(context);

    var result = await handler.Handle(new CallNextTicketCommand(counter.Id));

    Assert.Null(result);
  }

  [Fact]
  public async Task CallNextTicket_calls_the_oldest_waiting_ticket()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id, code: "P");
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    var t1 = await Seed.TicketAsync(context, queue.Id, flow.Id);
    var t2 = await Seed.TicketAsync(context, queue.Id, flow.Id);
    var handler = new CallNextTicketHandler(context);

    var result = await handler.Handle(new CallNextTicketCommand(counter.Id));

    Assert.NotNull(result);
    Assert.Equal(t1.Id, result.TicketId);
    Assert.Equal("P001", result.Code);

    var ticket = await context.Tickets.FindAsync(t1.Id);
    Assert.Equal(TicketStatus.InProgress, ticket!.Status);
    Assert.Equal(counter.Id, ticket.CounterId);
    Assert.NotNull(ticket.CalledAt);
  }

  [Fact]
  public async Task CallNextTicket_recalls_the_current_in_progress_ticket()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    await Seed.TicketAsync(context, queue.Id, flow.Id);
    var handler = new CallNextTicketHandler(context);

    var first = await handler.Handle(new CallNextTicketCommand(counter.Id));
    var second = await handler.Handle(new CallNextTicketCommand(counter.Id));

    Assert.Equal(first!.TicketId, second!.TicketId);
  }

  [Fact]
  public async Task CallNextTicket_prefers_the_highest_priority_queue()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var high = await Seed.QueueAsync(context, location.Id, code: "HIGH", priority: 10);
    var low = await Seed.QueueAsync(context, location.Id, code: "LOW", priority: 0);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [high, low]);
    await Seed.TicketAsync(context, high.Id, flow.Id);
    await Seed.TicketAsync(context, low.Id, flow.Id);
    var handler = new CallNextTicketHandler(context);

    var result = await handler.Handle(new CallNextTicketCommand(counter.Id));

    Assert.NotNull(result);
    Assert.Equal(high.Id, result.QueueId);
    Assert.StartsWith("HIGH", result.Code);
  }

  [Fact]
  public async Task CallNextTicket_applies_aging_bonus_to_old_tickets()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    // Cola A: prioridad 0 pero con aging. Cada 10 minutos sube 1 de prioridad (máx 5).
    var aging = await Seed.QueueAsync(context, location.Id, code: "OLD", priority: 0,
      agingIntervalMinutes: 10, maxAgingBonus: 5);
    // Cola B: prioridad 3, sin aging.
    var fresh = await Seed.QueueAsync(context, location.Id, code: "FRESH", priority: 3);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [aging, fresh]);

    // El ticket de la cola A lleva 60 minutos esperando -> bonus = min(6, 5) = 5 -> prioridad efectiva 5 > 3.
    await Seed.TicketAsync(context, aging.Id, flow.Id,
      createdAt: DateTime.UtcNow.AddMinutes(-60));
    await Seed.TicketAsync(context, fresh.Id, flow.Id);
    var handler = new CallNextTicketHandler(context);

    var result = await handler.Handle(new CallNextTicketCommand(counter.Id));

    Assert.NotNull(result);
    Assert.Equal(aging.Id, result.QueueId);
    Assert.StartsWith("OLD", result.Code);
  }

  [Fact]
  public async Task CallNextTicket_distributes_equally_by_weight()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var qA = await Seed.QueueAsync(context, location.Id, code: "A", priority: 5, weight: 1);
    var qB = await Seed.QueueAsync(context, location.Id, code: "B", priority: 5, weight: 1);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [qA, qB]);
    for (var i = 0; i < 20; i++)
    {
      await Seed.TicketAsync(context, qA.Id, flow.Id);
      await Seed.TicketAsync(context, qB.Id, flow.Id);
    }

    var callHandler = new CallNextTicketHandler(context);
    var attendHandler = new AttendTicketHandler(context);

    var calledQueues = new HashSet<Guid>();
    for (var i = 0; i < 30; i++)
    {
      var result = await callHandler.Handle(new CallNextTicketCommand(counter.Id));
      Assert.NotNull(result);
      calledQueues.Add(result.QueueId);
      await attendHandler.Handle(new AttendTicketCommand(counter.Id, result.TicketId));
    }

    Assert.Contains(qA.Id, calledQueues);
    Assert.Contains(qB.Id, calledQueues);
  }

  [Fact]
  public async Task CallNextTicket_throws_when_counter_not_found()
  {
    await using var context = _db.CreateContext();
    var handler = new CallNextTicketHandler(context);

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new CallNextTicketCommand(Guid.NewGuid())));

    Assert.Equal("Counter not found", ex.Message);
  }

  [Fact]
  public async Task AttendTicket_marks_the_ticket_as_attended()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);
    var handler = new AttendTicketHandler(context);

    await handler.Handle(new AttendTicketCommand(counter.Id, ticket.Id));

    var reloaded = await context.Tickets.FindAsync(ticket.Id);
    Assert.Equal(TicketStatus.Attended, reloaded!.Status);
    Assert.NotNull(reloaded.AttendedAt);
  }

  [Theory]
  [InlineData(true, false)]
  [InlineData(false, true)]
  public async Task AttendTicket_throws_when_counter_or_ticket_not_found(bool missingCounter, bool missingTicket)
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);
    var handler = new AttendTicketHandler(context);

    var counterId = missingCounter ? Guid.NewGuid() : counter.Id;
    var ticketId = missingTicket ? Guid.NewGuid() : ticket.Id;

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new AttendTicketCommand(counterId, ticketId)));
  }

  [Fact]
  public async Task CancelTicket_marks_the_ticket_as_canceled()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);
    var handler = new CancelTicketHandler(context);

    await handler.Handle(new CancelTicketCommand(ticket.Id));

    var reloaded = await context.Tickets.FindAsync(ticket.Id);
    Assert.Equal(TicketStatus.Canceled, reloaded!.Status);
    Assert.NotNull(reloaded.CanceledAt);
  }

  [Fact]
  public async Task CancelTicket_throws_when_ticket_not_found()
  {
    await using var context = _db.CreateContext();
    var handler = new CancelTicketHandler(context);

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new CancelTicketCommand(Guid.NewGuid())));
  }
}
