using Keues.Application.Features.Queues.CreateNewTicket;
using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.UseCases;

public class TicketConcurrencyTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  [Fact]
  public async Task Concurrent_creation_produces_unique_codes()
  {
    await using var seedContext = _db.CreateContext();
    var location = await Seed.LocationAsync(seedContext);
    var flow = await Seed.FlowAsync(seedContext, location.Id);
    var queue = await Seed.QueueAsync(seedContext, location.Id, code: "C");

    var tasks = Enumerable.Range(0, 20).Select(async _ =>
    {
      await using var context = _db.CreateContext();
      var handler = new CreateNewTicketHandler(context, new FakeEventPublisher());
      return await handler.Handle(new CreateNewTicketCommand(queue.Id, flow.Id));
    });

    var responses = await Task.WhenAll(tasks);
    var codes = responses.Select(response => response.Code).OrderBy(code => code).ToList();

    Assert.Equal(20, codes.Distinct().Count());
    Assert.Equal(Enumerable.Range(1, 20).Select(number => $"C{number:000}"), codes);
  }

  [Fact]
  public async Task Concurrent_creation_refreshes_a_queue_preloaded_in_each_context()
  {
    await using var seedContext = _db.CreateContext();
    var location = await Seed.LocationAsync(seedContext);
    var flow = await Seed.FlowAsync(seedContext, location.Id);
    var queue = await Seed.QueueAsync(seedContext, location.Id, code: "C");

    await using var firstContext = _db.CreateContext();
    await using var secondContext = _db.CreateContext();
    await firstContext.Queues.FindAsync(queue.Id);
    await secondContext.Queues.FindAsync(queue.Id);

    var firstHandler = new CreateNewTicketHandler(firstContext, new FakeEventPublisher());
    var secondHandler = new CreateNewTicketHandler(secondContext, new FakeEventPublisher());

    var responses = await Task.WhenAll(
      firstHandler.Handle(new CreateNewTicketCommand(queue.Id, flow.Id)),
      secondHandler.Handle(new CreateNewTicketCommand(queue.Id, flow.Id)));

    Assert.Equal(2, responses.Select(response => response.Code).Distinct().Count());
  }
}
