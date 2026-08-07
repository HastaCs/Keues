using Keues.Application.Features.Counters;
using Keues.Application.Features.Counters.GetAllCounters;
using Keues.Application.Features.Flows;
using Keues.Application.Features.Flows.GetAllFlows;
using Keues.Application.Features.Locations;
using Keues.Application.Features.Locations.CreateLocation;
using Keues.Application.Features.Locations.DeleteLocation;
using Keues.Application.Features.Locations.GetAllLocations;
using Keues.Application.Features.Locations.GetLocation;
using Keues.Application.Features.Locations.UpdateLocation;
using Keues.Application.Features.Queues.GetAllQueues;
using Keues.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Keues.Tests.UseCases;

public class LocationUseCasesTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  [Fact]
  public async Task Create_creates_and_returns_the_location()
  {
    await using var context = _db.CreateContext();
    var handler = new CreateLocationHandler(context);

    var response = await handler.Handle(new CreateLocationCommand
    {
      Name = "Tienda",
      Description = "Tienda central",
      Color = "red"
    });

    Assert.NotEqual(Guid.Empty, response.Id);
    Assert.Equal("Tienda", response.Name);
    Assert.Equal("red", response.Color);
  }

  [Fact]
  public async Task Get_returns_the_location()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var handler = new GetLocationHandler(context);

    var response = await handler.Handle(location.Id);

    Assert.Equal(location.Id, response.Id);
    Assert.Equal("Tienda Central", response.Name);
  }

  [Fact]
  public async Task Get_throws_when_not_found()
  {
    await using var context = _db.CreateContext();
    var handler = new GetLocationHandler(context);

    await Assert.ThrowsAsync<Exception>(() => handler.Handle(Guid.NewGuid()));
  }

  [Fact]
  public async Task GetAll_returns_all_locations()
  {
    await using var context = _db.CreateContext();
    await Seed.LocationAsync(context, "A");
    await Seed.LocationAsync(context, "B");
    var handler = new GetAllLocationsHandler(context);

    var response = await handler.Handle();

    Assert.Equal(2, response.Count());
  }

  [Fact]
  public async Task Update_updates_the_location()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var handler = new UpdateLocationHandler(context);

    var response = await handler.Handle(new UpdateLocationCommand
    {
      Id = location.Id,
      Name = "Renombrada",
      Description = "Nueva descripción",
      Color = "green"
    });

    Assert.Equal("Renombrada", response.Name);
    Assert.Equal("green", response.Color);
  }

  [Fact]
  public async Task Delete_cascades_soft_delete_to_queues_counters_and_flows()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    var handler = new DeleteLocationHandler(context);

    await handler.Handle(new DeleteLocationCommand(location.Id));

    Assert.Empty(await new GetAllLocationsHandler(context).Handle());
    Assert.Empty(await new GetAllQueuesHandler(context).Handle(new GetAllQueuesCommand()));
    Assert.Empty(await new GetAllCountersHandler(context).Handle(new GetAllCountersCommand()));
    Assert.Empty(await new GetAllFlowsHandler(context).Handle(new GetAllFlowsCommand(null)));

    // Los registros subyacentes conservan el RemovedAt (soft delete).
    var rawQueues = await context.Queues.IgnoreQueryFilters().ToListAsync();
    Assert.All(rawQueues, q => Assert.NotNull(q.RemovedAt));
    Assert.NotNull(flow.RemovedAt);
    Assert.NotNull(counter.RemovedAt);
  }
}
