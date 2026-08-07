using Keues.Application.Features.Queues;
using Keues.Application.Features.Queues.CreateNewTicket;
using Keues.Application.Features.Queues.CreateQueue;
using Keues.Application.Features.Queues.DeleteQueue;
using Keues.Application.Features.Queues.GetAllQueues;
using Keues.Application.Features.Queues.GetQueue;
using Keues.Application.Features.Queues.UpdateQueue;
using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.UseCases;

public class QueueUseCasesTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  private CreateQueueCommand NewCommand(Guid locationId, string code = "P", int? maxValue = null) => new()
  {
    Name = "Pescadería",
    Description = "Cola de pescadería",
    Code = code,
    MaxValue = maxValue,
    LocationId = locationId,
    Priority = 5,
    Weight = 2,
    AgingIntervalMinutes = 10,
    MaxAgingBonus = 3,
    Color = "blue",
    Counters = []
  };

  [Fact]
  public async Task Create_creates_and_returns_the_queue()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var handler = new CreateQueueHandler(context);

    var response = await handler.Handle(NewCommand(location.Id));

    Assert.NotEqual(Guid.Empty, response.Id);
    Assert.Equal("Pescadería", response.Name);
    Assert.Equal("P", response.Code);
    Assert.Equal(5, response.Priority);
    Assert.Equal(2, response.Weight);
  }

  [Fact]
  public async Task CreateNewTicket_generates_sequential_codes()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id, code: "P");
    var handler = new CreateNewTicketHandler(context);

    var first = await handler.Handle(new CreateNewTicketCommand(queue.Id, flow.Id));
    var second = await handler.Handle(new CreateNewTicketCommand(queue.Id, flow.Id));
    var third = await handler.Handle(new CreateNewTicketCommand(queue.Id, flow.Id));

    Assert.Equal("P001", first.Code);
    Assert.Equal("P002", second.Code);
    Assert.Equal("P003", third.Code);

    var reloaded = await context.Queues.FindAsync(queue.Id);
    Assert.Equal(4, reloaded!.NextNumber);
  }

  [Fact]
  public async Task CreateNewTicket_resets_numbering_at_max_value()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id, code: "P");
    queue.MaxValue = 2;
    await context.SaveChangesAsync();
    var handler = new CreateNewTicketHandler(context);

    var first = await handler.Handle(new CreateNewTicketCommand(queue.Id, flow.Id));
    var second = await handler.Handle(new CreateNewTicketCommand(queue.Id, flow.Id));
    var third = await handler.Handle(new CreateNewTicketCommand(queue.Id, flow.Id));

    Assert.Equal("P001", first.Code);
    Assert.Equal("P002", second.Code);
    Assert.Equal("P001", third.Code);
  }

  [Fact]
  public async Task CreateNewTicket_throws_when_queue_not_found()
  {
    await using var context = _db.CreateContext();
    var handler = new CreateNewTicketHandler(context);

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new CreateNewTicketCommand(Guid.NewGuid(), Guid.NewGuid())));

    Assert.Equal("Ticket type not found", ex.Message);
  }

  [Fact]
  public async Task Get_returns_the_queue()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var queue = await Seed.QueueAsync(context, location.Id, code: "P", name: "Pescadería");
    var handler = new GetQueueHandler(context);

    var response = await handler.Handle(new GetQueueCommand(queue.Id));

    Assert.Equal(queue.Id, response.Id);
    Assert.Equal("Pescadería", response.Name);
    Assert.Equal("P", response.Code);
  }

  [Fact]
  public async Task Get_throws_when_not_found()
  {
    await using var context = _db.CreateContext();
    var handler = new GetQueueHandler(context);

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new GetQueueCommand(Guid.NewGuid())));
  }

  [Fact]
  public async Task GetAll_filters_by_location()
  {
    await using var context = _db.CreateContext();
    var locA = await Seed.LocationAsync(context, "A");
    var locB = await Seed.LocationAsync(context, "B");
    await Seed.QueueAsync(context, locA.Id, code: "A1");
    await Seed.QueueAsync(context, locA.Id, code: "A2");
    await Seed.QueueAsync(context, locB.Id, code: "B1");
    var handler = new GetAllQueuesHandler(context);

    var onlyA = await handler.Handle(new GetAllQueuesCommand { LocationId = locA.Id });

    Assert.Equal(2, onlyA.Count());

    var all = await handler.Handle(new GetAllQueuesCommand());
    Assert.Equal(3, all.Count());
  }

  [Fact]
  public async Task Update_updates_fields_and_replaces_counters()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var queue = await Seed.QueueAsync(context, location.Id, code: "P");
    var otherCounter = await Seed.CounterAsync(context, location.Id, code: "C9");
    var handler = new UpdateQueueHandler(context);

    var response = await handler.Handle(new UpdateQueueCommand
    {
      Id = queue.Id,
      Name = "Pescadería nueva",
      Description = "Nueva desc",
      Code = "F",
      MaxValue = 50,
      LocationId = location.Id,
      Priority = 9,
      Weight = 4,
      AgingIntervalMinutes = 20,
      MaxAgingBonus = 7,
      Color = "red",
      Counters = [otherCounter.Id]
    });

    Assert.Equal("Pescadería nueva", response.Name);
    Assert.Equal("F", response.Code);
    Assert.Equal(9, response.Priority);
    Assert.Equal([otherCounter.Id], response.Counters);
  }

  [Fact]
  public async Task Update_throws_when_not_found()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var handler = new UpdateQueueHandler(context);

    var command = new UpdateQueueCommand
    {
      Id = Guid.NewGuid(),
      Name = "Pescadería",
      Description = "Cola de pescadería",
      Code = "P",
      LocationId = location.Id,
      Priority = 5,
      Weight = 2,
      AgingIntervalMinutes = 10,
      MaxAgingBonus = 3,
      Color = "blue",
      Counters = []
    };
    await Assert.ThrowsAsync<Exception>(() => handler.Handle(command));
  }

  [Fact]
  public async Task Delete_soft_deletes_the_queue()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var queue = await Seed.QueueAsync(context, location.Id);
    var deleteHandler = new DeleteQueueHandler(context);
    var getAllHandler = new GetAllQueuesHandler(context);
    var getHandler = new GetQueueHandler(context);

    await deleteHandler.Handle(new DeleteQueueCommand(queue.Id));

    Assert.Empty(await getAllHandler.Handle(new GetAllQueuesCommand()));
    await Assert.ThrowsAsync<Exception>(() => getHandler.Handle(new GetQueueCommand(queue.Id)));
  }
}
