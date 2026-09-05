using Keues.Application.Features.Counters.AttendTicket;
using Keues.Application.Features.Counters.CallNextTicket;
using Keues.Application.Features.Counters.CancelTicket;
using Keues.Application.Features.Counters.TransferTicket;
using Keues.Application.Features.Queues.CreateNewTicket;
using Keues.Application.Features.Tickets.GetTicketHistory;
using Keues.Domain.Events;
using Keues.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Keues.Tests.UseCases;

public class TicketHistoryUseCasesTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  [Fact]
  public async Task GetTicketHistory_returns_empty_when_ticket_has_no_history()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);
    var handler = new GetTicketHistoryHandler(context);

    var result = await handler.Handle(new GetTicketRequest(ticket.Id));

    Assert.Empty(result);
  }

  [Fact]
  public async Task GetTicketHistory_returns_empty_when_ticket_does_not_exist()
  {
    await using var context = _db.CreateContext();
    var handler = new GetTicketHistoryHandler(context);

    var result = await handler.Handle(new GetTicketRequest(Guid.NewGuid()));

    Assert.Empty(result);
  }

  [Fact]
  public async Task CreateNewTicket_records_a_created_event_without_counter()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id, code: "P");
    var handler = new CreateNewTicketHandler(context);

    var response = await handler.Handle(new CreateNewTicketCommand(queue.Id, flow.Id));

    var history = await context.TicketHistories.SingleAsync(h => h.TicketId == response.Id);
    Assert.Equal(KeuesEventsType.Ticket.Created, history.Event);
    Assert.Null(history.CounterId);
  }

  [Fact]
  public async Task GetTicketHistory_returns_created_event_without_counter_name()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id, code: "P");
    var ticketId = await new CreateNewTicketHandler(context)
      .Handle(new CreateNewTicketCommand(queue.Id, flow.Id));
    var handler = new GetTicketHistoryHandler(context);

    var result = await handler.Handle(new GetTicketRequest(ticketId.Id));

    var single = Assert.Single(result);
    Assert.Equal(KeuesEventsType.Ticket.Created, single.Event);
    Assert.Null(single.CounterName);
  }

  [Fact]
  public async Task CallNextTicket_records_a_called_event_with_counter()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id, code: "P", name: "Pescadería");
    var counter = await Seed.CounterAsync(context, location.Id, code: "C1", name: "Caja 1", queues: [queue]);
    await Seed.TicketAsync(context, queue.Id, flow.Id);
    var handler = new CallNextTicketHandler(context);

    var result = await handler.Handle(new CallNextTicketCommand(counter.Id));

    Assert.NotNull(result);
    var history = await context.TicketHistories.SingleAsync(h => h.TicketId == result.TicketId);
    Assert.Equal(KeuesEventsType.Ticket.Called, history.Event);
    Assert.Equal(counter.Id, history.CounterId);
  }

  [Fact]
  public async Task GetTicketHistory_returns_called_event_with_counter_name()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id, code: "P", name: "Pescadería");
    var counter = await Seed.CounterAsync(context, location.Id, code: "C1", name: "Caja 1", queues: [queue]);
    await Seed.TicketAsync(context, queue.Id, flow.Id);
    await new CallNextTicketHandler(context).Handle(new CallNextTicketCommand(counter.Id));
    var handler = new GetTicketHistoryHandler(context);

    var history = await context.TicketHistories.SingleAsync();
    var result = await handler.Handle(new GetTicketRequest(history.TicketId));

    var single = Assert.Single(result);
    Assert.Equal(KeuesEventsType.Ticket.Called, single.Event);
    Assert.Equal("Caja 1", single.CounterName);
  }

  [Fact]
  public async Task AttendTicket_records_an_attended_event_with_counter()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);
    var handler = new AttendTicketHandler(context);

    await handler.Handle(new AttendTicketCommand(counter.Id, ticket.Id));

    var history = await context.TicketHistories.SingleAsync(h => h.TicketId == ticket.Id);
    Assert.Equal(KeuesEventsType.Ticket.Attended, history.Event);
    Assert.Equal(counter.Id, history.CounterId);
  }

  [Fact]
  public async Task CancelTicket_records_a_canceled_event_with_counter()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);
    var handler = new CancelTicketHandler(context);

    await handler.Handle(new CancelTicketCommand(ticket.Id, counter.Id));

    var history = await context.TicketHistories.SingleAsync(h => h.TicketId == ticket.Id);
    Assert.Equal(KeuesEventsType.Ticket.Canceled, history.Event);
    Assert.Equal(counter.Id, history.CounterId);
  }

  [Fact]
  public async Task TransferTicket_records_a_transferred_event_with_counter()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var sourceQueue = await Seed.QueueAsync(context, location.Id, code: "P");
    var destQueue = await Seed.QueueAsync(context, location.Id, code: "F");
    var counter = await Seed.CounterAsync(context, location.Id, queues: [sourceQueue]);
    var ticket = await Seed.TicketAsync(context, sourceQueue.Id, flow.Id);
    var handler = new TransferTicketHandler(context);

    await handler.Handle(new TransferTicketCommand(counter.Id, ticket.Id, destQueue.Id));

    var history = await context.TicketHistories.SingleAsync(h => h.TicketId == ticket.Id);
    Assert.Equal(KeuesEventsType.Ticket.Transferred, history.Event);
    Assert.Equal(counter.Id, history.CounterId);
  }

  [Fact]
  public async Task GetTicketHistory_orders_events_by_created_at_ascending()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id, code: "P");
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    var createHandler = new CreateNewTicketHandler(context);
    var callHandler = new CallNextTicketHandler(context);

    var ticketId = (await createHandler.Handle(new CreateNewTicketCommand(queue.Id, flow.Id))).Id;
    await callHandler.Handle(new CallNextTicketCommand(counter.Id));
    var handler = new GetTicketHistoryHandler(context);

    var result = await handler.Handle(new GetTicketRequest(ticketId));

    Assert.Equal([KeuesEventsType.Ticket.Created, KeuesEventsType.Ticket.Called],
      result.Select(r => r.Event).ToArray());
  }
}
