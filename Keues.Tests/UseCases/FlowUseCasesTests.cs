using Keues.Application.Features.Flows;
using Keues.Application.Features.Flows.CreateFlow;
using Keues.Application.Features.Flows.DeleteFlow;
using Keues.Application.Features.Flows.GetAllFlows;
using Keues.Application.Features.Flows.GetFlow;
using Keues.Application.Features.Flows.UpdateFlow;
using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.UseCases;

public class FlowUseCasesTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  [Fact]
  public async Task Create_creates_a_flow_for_an_existing_location()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var handler = new CreateFlowHandler(context);

    var response = await handler.Handle(new CreateFlowCommand(
      "Flujo principal", "Descripción", FlowType.TicketMachine, location.Id, "{}"));

    Assert.NotEqual(Guid.Empty, response.Id);
    Assert.Equal("Flujo principal", response.Name);
    Assert.Equal(FlowType.TicketMachine, response.FlowType);
    Assert.Equal(location.Id, response.Location.Id);
  }

  [Fact]
  public async Task Create_throws_when_location_not_found()
  {
    await using var context = _db.CreateContext();
    var handler = new CreateFlowHandler(context);

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new CreateFlowCommand(
        "Flujo", "Desc", FlowType.SetFree, Guid.NewGuid(), "{}")));
  }

  [Fact]
  public async Task Get_returns_the_flow()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id, name: "Flujo A");
    var handler = new GetFlowHandler(context);

    var response = await handler.Handle(new GetFlowCommand(flow.Id));

    Assert.Equal(flow.Id, response.Id);
    Assert.Equal("Flujo A", response.Name);
  }

  [Fact]
  public async Task GetAll_filters_by_location()
  {
    await using var context = _db.CreateContext();
    var locA = await Seed.LocationAsync(context, "A");
    var locB = await Seed.LocationAsync(context, "B");
    await Seed.FlowAsync(context, locA.Id);
    await Seed.FlowAsync(context, locA.Id);
    await Seed.FlowAsync(context, locB.Id);
    var handler = new GetAllFlowsHandler(context);

    var onlyA = await handler.Handle(new GetAllFlowsCommand(locA.Id));
    var all = await handler.Handle(new GetAllFlowsCommand(null));

    Assert.Equal(2, onlyA.Count());
    Assert.Equal(3, all.Count());
  }

  [Fact]
  public async Task Update_updates_the_flow()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var handler = new UpdateFlowHandler(context);

    var response = await handler.Handle(new UpdateFlowCommand(flow.Id, "Nuevo", "Nueva desc", "{\"x\":1}"));

    Assert.Equal("Nuevo", response.Name);
    Assert.Equal("Nueva desc", response.Description);
  }

  [Fact]
  public async Task Delete_soft_deletes_the_flow()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var handler = new DeleteFlowHandler(context);

    await handler.Handle(new DeleteFlowCommand(flow.Id));

    var getAll = await new GetAllFlowsHandler(context).Handle(new GetAllFlowsCommand(null));
    Assert.Empty(getAll);
  }
}
