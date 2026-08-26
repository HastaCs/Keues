using Keues.Application.Features.Tickets.GetAllTickets;
using Keues.Application.Features.Tickets.GetTicket;
using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.UseCases;

public class TicketUseCasesTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  [Fact]
  public async Task GetTicket_returns_the_ticket_with_relations()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id, name: "Flujo");
    var queue = await Seed.QueueAsync(context, location.Id, code: "P", name: "Pescadería");
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);
    ticket.CounterId = counter.Id;
    await context.SaveChangesAsync();
    var handler = new GetTicketHandler(context);

    var response = await handler.Handle(new GetTicketCommand(ticket.Id));

    Assert.Equal(ticket.Id, response.Id);
    Assert.Equal("P001", response.Code);
    Assert.Equal(TicketStatus.Waiting, response.Status);
    Assert.Equal(queue.Id, response.Queue!.Id);
    Assert.Equal("Pescadería", response.Queue.Name);
    Assert.Equal(counter.Id, response.Counter!.Id);
    Assert.Equal(flow.Id, response.Flow.Id);
    Assert.Equal(location.Id, response.LocationId);
  }

  [Fact]
  public async Task GetTicket_throws_when_not_found()
  {
    await using var context = _db.CreateContext();
    var handler = new GetTicketHandler(context);

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new GetTicketCommand(Guid.NewGuid())));
  }

  [Fact]
  public async Task GetAllTickets_returns_empty_when_no_tickets()
  {
    await using var context = _db.CreateContext();
    var handler = new GetAllTicketsHandler(context);

    var result = await handler.Handle(new GetAllTicketsCommand());

    Assert.Empty(result.Tickets);
  }

  [Fact]
  public async Task GetAllTickets_filters_by_status_and_code()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id, code: "P");
    var waiting = await Seed.TicketAsync(context, queue.Id, flow.Id);
    await Seed.TicketAsync(context, queue.Id, flow.Id, status: TicketStatus.Canceled);
    var handler = new GetAllTicketsHandler(context);

    var waitingOnly = await handler.Handle(new GetAllTicketsCommand
    {
      Status = TicketStatus.Waiting
    });
    var byCode = await handler.Handle(new GetAllTicketsCommand
    {
      Code = waiting.Code
    });

    Assert.Single(waitingOnly.Tickets);
    Assert.Equal(waiting.Id, waitingOnly.Tickets.Single().Id);
    Assert.Single(byCode.Tickets);
  }

  [Fact]
  public async Task GetAllTickets_filters_by_location_queue_and_date_range()
  {
    await using var context = _db.CreateContext();
    var locA = await Seed.LocationAsync(context, "A");
    var locB = await Seed.LocationAsync(context, "B");
    var flowA = await Seed.FlowAsync(context, locA.Id);
    var flowB = await Seed.FlowAsync(context, locB.Id);
    var queueA = await Seed.QueueAsync(context, locA.Id, code: "A");
    var queueB = await Seed.QueueAsync(context, locB.Id, code: "B");
    await Seed.TicketAsync(context, queueA.Id, flowA.Id);
    await Seed.TicketAsync(context, queueA.Id, flowA.Id);
    await Seed.TicketAsync(context, queueB.Id, flowB.Id);
    var handler = new GetAllTicketsHandler(context);

    var byLocation = await handler.Handle(new GetAllTicketsCommand { LocationId = locA.Id });
    var byQueue = await handler.Handle(new GetAllTicketsCommand { QueueId = queueB.Id });
    var byRange = await handler.Handle(new GetAllTicketsCommand
    {
      CreatedFrom = DateTime.UtcNow.AddMinutes(-1),
      CreatedTo = DateTime.UtcNow.AddMinutes(1)
    });

    Assert.Equal(2, byLocation.Tickets.Count());
    Assert.Single(byQueue.Tickets);
    Assert.Equal(3, byRange.Tickets.Count());
  }

  [Fact]
  public async Task GetAllTickets_sorts_by_created_at_ascending_and_descending()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id, code: "P");
    var first = await Seed.TicketAsync(context, queue.Id, flow.Id, createdAt: DateTime.UtcNow.AddDays(-2));
    var second = await Seed.TicketAsync(context, queue.Id, flow.Id, createdAt: DateTime.UtcNow.AddDays(-1));
    var handler = new GetAllTicketsHandler(context);

    var ascending = await handler.Handle(new GetAllTicketsCommand { SortOrder = SortOrder.Asc });
    var descending = await handler.Handle(new GetAllTicketsCommand { SortOrder = SortOrder.Desc });

    Assert.Equal([first.Id, second.Id], ascending.Tickets.Select(t => t.Id).ToArray());
    Assert.Equal([second.Id, first.Id], descending.Tickets.Select(t => t.Id).ToArray());
  }
}
