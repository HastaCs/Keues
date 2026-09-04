using Keues.Application.Features.Counters.TransferTicket;
using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Keues.Tests.UseCases;

public class TransferTicketUseCasesTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  [Fact]
  public async Task Transfer_moves_ticket_to_destination_queue_and_puts_it_waiting()
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

    var reloaded = await context.Tickets.Include(t => t.Queue).FirstAsync(t => t.Id == ticket.Id);
    Assert.Equal(destQueue.Id, reloaded.QueueId);
    Assert.Equal(destQueue.Name, reloaded.Queue.Name);
    Assert.Equal(TicketStatus.Waiting, reloaded.Status);
  }

  [Fact]
  public async Task Transfer_resets_an_in_progress_ticket_to_waiting()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var sourceQueue = await Seed.QueueAsync(context, location.Id, code: "P");
    var destQueue = await Seed.QueueAsync(context, location.Id, code: "F");
    var counter = await Seed.CounterAsync(context, location.Id, queues: [sourceQueue]);
    var ticket = await Seed.TicketAsync(context, sourceQueue.Id, flow.Id);
    ticket.Status = TicketStatus.InProgress;
    ticket.CounterId = counter.Id;
    ticket.CalledAt = DateTime.UtcNow;
    await context.SaveChangesAsync();
    var handler = new TransferTicketHandler(context);

    await handler.Handle(new TransferTicketCommand(counter.Id, ticket.Id, destQueue.Id));

    var reloaded = await context.Tickets.Include(t => t.Queue).FirstAsync(t => t.Id == ticket.Id);
    Assert.Equal(destQueue.Id, reloaded.QueueId);
    Assert.Equal(TicketStatus.Waiting, reloaded.Status);
    Assert.Null(reloaded.CalledAt);
  }

  [Fact]
  public async Task Transfer_throws_when_counter_not_found()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);
    var handler = new TransferTicketHandler(context);
    var missingCounterId = Guid.NewGuid();

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new TransferTicketCommand(missingCounterId, ticket.Id, queue.Id)));

    Assert.Equal($"Counter with Id {missingCounterId} not found.", ex.Message);
  }

  [Fact]
  public async Task Transfer_throws_when_ticket_not_found()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    var handler = new TransferTicketHandler(context);
    var missingTicketId = Guid.NewGuid();

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new TransferTicketCommand(counter.Id, missingTicketId, queue.Id)));

    Assert.Equal($"Ticket with Id {missingTicketId} not found.", ex.Message);
  }

  [Fact]
  public async Task Transfer_throws_when_destination_queue_not_found()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var sourceQueue = await Seed.QueueAsync(context, location.Id, code: "P");
    var counter = await Seed.CounterAsync(context, location.Id, queues: [sourceQueue]);
    var ticket = await Seed.TicketAsync(context, sourceQueue.Id, flow.Id);
    var handler = new TransferTicketHandler(context);
    var missingQueueId = Guid.NewGuid();

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new TransferTicketCommand(counter.Id, ticket.Id, missingQueueId)));

    Assert.Equal($"Queue with Id {missingQueueId} not found.", ex.Message);
  }

  [Fact]
  public async Task Transfer_throws_when_destination_queue_is_in_another_location()
  {
    await using var context = _db.CreateContext();
    var locationA = await Seed.LocationAsync(context, "A");
    var locationB = await Seed.LocationAsync(context, "B");
    var flow = await Seed.FlowAsync(context, locationA.Id);
    var sourceQueue = await Seed.QueueAsync(context, locationA.Id, code: "P", name: "Cola P");
    var destQueue = await Seed.QueueAsync(context, locationB.Id, code: "F", name: "Cola F");
    var counter = await Seed.CounterAsync(context, locationA.Id, queues: [sourceQueue]);
    var ticket = await Seed.TicketAsync(context, sourceQueue.Id, flow.Id);
    var handler = new TransferTicketHandler(context);

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new TransferTicketCommand(counter.Id, ticket.Id, destQueue.Id)));

    Assert.Equal("Queue Cola F is not in the same location as the ticket's queue.", ex.Message);
  }
}