using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.Api;

public class FlowsApiTests : ApiTestBase
{
  [Fact]
  public async Task Create_returns_the_flow()
  {
    var client = await CreateAuthenticatedClientAsync();
    var location = await client.ReadAsync<LocationBody>(
      await client.PostAsync("/api/locations", new { name = "Tienda", description = "", color = "blue" }));

    var response = await client.PostAsync("/api/flows", new
    {
      name = "Flujo principal",
      description = "Descripción",
      flowType = (int)FlowType.TicketMachine,
      locationId = location!.Id,
      flowJson = "{}"
    });

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<FlowBody>(response);
    Assert.NotEqual(Guid.Empty, body!.Id);
    Assert.Equal("Flujo principal", body.Name);
    Assert.Equal((int)FlowType.TicketMachine, body.FlowType);
  }

  [Fact]
  public async Task Create_returns_401_without_authentication()
  {
    var client = Factory.CreateTestClient();

    var response = await client.PostAsync("/api/flows", new
    {
      name = "Flujo",
      description = "",
      flowType = (int)FlowType.SetFree,
      locationId = Guid.NewGuid(),
      flowJson = "{}"
    });

    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Create_with_unknown_location_returns_400()
  {
    var client = await CreateAuthenticatedClientAsync();

    var response = await client.PostAsync("/api/flows", new
    {
      name = "Flujo",
      description = "",
      flowType = (int)FlowType.SetFree,
      locationId = Guid.NewGuid(),
      flowJson = "{}"
    });

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task GetAll_filters_by_location()
  {
    var client = await CreateAuthenticatedClientAsync();
    var locA = await client.ReadAsync<LocationBody>(
      await client.PostAsync("/api/locations", new { name = "A", description = "", color = "blue" }));
    var locB = await client.ReadAsync<LocationBody>(
      await client.PostAsync("/api/locations", new { name = "B", description = "", color = "blue" }));
    await client.PostAsync("/api/flows", new { name = "F1", description = "", flowType = (int)FlowType.TicketMachine, locationId = locA!.Id, flowJson = "{}" });
    await client.PostAsync("/api/flows", new { name = "F2", description = "", flowType = (int)FlowType.TicketMachine, locationId = locA.Id, flowJson = "{}" });
    await client.PostAsync("/api/flows", new { name = "F3", description = "", flowType = (int)FlowType.ManualCall, locationId = locB!.Id, flowJson = "{}" });

    var response = await client.GetAsync($"/api/flows?locationId={locA.Id}");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<DataBody<FlowBody>>(response);
    Assert.Equal(2, body!.Data.Count);
  }

  [Fact]
  public async Task Update_returns_the_updated_flow()
  {
    var client = await CreateAuthenticatedClientAsync();
    var location = await client.ReadAsync<LocationBody>(
      await client.PostAsync("/api/locations", new { name = "Tienda", description = "", color = "blue" }));
    var flow = await client.ReadAsync<FlowBody>(
      await client.PostAsync("/api/flows", new { name = "Flujo", description = "", flowType = (int)FlowType.TicketMachine, locationId = location!.Id, flowJson = "{}" }));

    var response = await client.PutAsync($"/api/flows/{flow!.Id}", new
    {
      name = "Flujo renombrado",
      description = "Nueva",
      flowJson = "{\"x\":1}"
    });

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<UpdateFlowBody>(response);
    Assert.Equal("Flujo renombrado", body!.Name);
  }

  [Fact]
  public async Task Update_returns_401_without_authentication()
  {
    var client = Factory.CreateTestClient();

    var response = await client.PutAsync($"/api/flows/{Guid.NewGuid()}", new
    {
      name = "Flujo",
      description = "",
      flowJson = "{}"
    });

    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Delete_returns_200_and_hides_the_flow()
  {
    var client = await CreateAuthenticatedClientAsync();
    var location = await client.ReadAsync<LocationBody>(
      await client.PostAsync("/api/locations", new { name = "Tienda", description = "", color = "blue" }));
    var flow = await client.ReadAsync<FlowBody>(
      await client.PostAsync("/api/flows", new { name = "Flujo", description = "", flowType = (int)FlowType.TicketMachine, locationId = location!.Id, flowJson = "{}" }));

    var response = await client.DeleteAsync($"/api/flows/{flow!.Id}");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var get = await client.GetAsync($"/api/flows/{flow.Id}");
    Assert.Equal(System.Net.HttpStatusCode.BadRequest, get.StatusCode);
  }

  [Fact]
  public async Task Delete_returns_401_without_authentication()
  {
    var client = Factory.CreateTestClient();

    var response = await client.DeleteAsync($"/api/flows/{Guid.NewGuid()}");

    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
  }

  private sealed record LocationBody(Guid Id, string Name, string? Description, string Color);
  private sealed record FlowBody(Guid Id, string Name, string Description, int FlowType);
  private sealed record UpdateFlowBody(Guid Id, string Name, string Description);
  private sealed record DataBody<T>(List<T> Data);
}
