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
  public async Task Create_QrVirtual_creates_the_flow_with_its_queue()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var queue = await Seed.QueueAsync(context, location.Id);
    var handler = new CreateFlowHandler(context);
    var flowJson = FlowJsonWithQueue(queue.Id);

    var response = await handler.Handle(new CreateFlowCommand(
      "QR virtual", "Descripción", FlowType.QrVirtual, location.Id, flowJson));

    Assert.Equal(FlowType.QrVirtual, response.FlowType);
    Assert.Equal(flowJson, response.FlowJson);
  }

  [Fact]
  public async Task Create_QrVirtual_without_queue_throws()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var handler = new CreateFlowHandler(context);

    var exception = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new CreateFlowCommand(
        "QR virtual", "Descripción", FlowType.QrVirtual, location.Id, "[]")));

    Assert.Contains("queue", exception.Message);
  }

  [Fact]
  public async Task Create_QrVirtual_with_queue_from_another_location_throws()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context, "Origen");
    var otherLocation = await Seed.LocationAsync(context, "Otra");
    var otherQueue = await Seed.QueueAsync(context, otherLocation.Id);
    var handler = new CreateFlowHandler(context);

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new CreateFlowCommand(
        "QR virtual", "Descripción", FlowType.QrVirtual, location.Id, FlowJsonWithQueue(otherQueue.Id))));
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
  public async Task Update_QrVirtual_updates_the_flow_with_a_valid_queue()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var firstQueue = await Seed.QueueAsync(context, location.Id, code: "A");
    var secondQueue = await Seed.QueueAsync(context, location.Id, code: "B");
    var flow = await Seed.FlowAsync(context, location.Id, FlowType.QrVirtual, "QR");
    flow.FlowJson = FlowJsonWithQueue(firstQueue.Id);
    await context.SaveChangesAsync();
    var handler = new UpdateFlowHandler(context);
    var newFlowJson = FlowJsonWithQueue(secondQueue.Id);

    var response = await handler.Handle(new UpdateFlowCommand(flow.Id, "Nuevo", "Nueva desc", newFlowJson));

    Assert.Equal("Nuevo", response.Name);
    var updated = await context.Flows.FindAsync(flow.Id);
    Assert.Equal(FlowType.QrVirtual, updated!.FlowType);
    Assert.Equal(newFlowJson, updated.FlowJson);
  }

  [Fact]
  public async Task Update_QrVirtual_without_queue_throws()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var queue = await Seed.QueueAsync(context, location.Id);
    var flow = await Seed.FlowAsync(context, location.Id, FlowType.QrVirtual, "QR");
    flow.FlowJson = FlowJsonWithQueue(queue.Id);
    await context.SaveChangesAsync();
    var handler = new UpdateFlowHandler(context);

    var exception = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new UpdateFlowCommand(flow.Id, "Nuevo", "Nueva desc", "[]")));

    Assert.Contains("queue", exception.Message);
  }

  [Fact]
  public async Task Update_QrVirtual_with_queue_from_another_location_throws()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context, "Origen");
    var otherLocation = await Seed.LocationAsync(context, "Otra");
    var queue = await Seed.QueueAsync(context, location.Id);
    var otherQueue = await Seed.QueueAsync(context, otherLocation.Id);
    var flow = await Seed.FlowAsync(context, location.Id, FlowType.QrVirtual, "QR");
    flow.FlowJson = FlowJsonWithQueue(queue.Id);
    await context.SaveChangesAsync();
    var handler = new UpdateFlowHandler(context);

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new UpdateFlowCommand(flow.Id, "Nuevo", "Nueva desc", FlowJsonWithQueue(otherQueue.Id))));
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

  private static string FlowJsonWithQueue(Guid queueId) => $$"""
    [
      {
        "nodeType": "ticket",
        "queueId": "{{queueId}}"
      }
    ]
    """;
}
