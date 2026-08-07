using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.Api;

public class QueuesApiTests : ApiTestBase
{
  [Fact]
  public async Task Create_returns_the_queue()
  {
    var client = await CreateAuthenticatedClientAsync();
    var location = await client.ReadAsync<LocationBody>(
      await client.PostAsync("/api/locations", new { name = "Tienda", description = "", color = "blue" }));

    var response = await client.PostAsync("/api/queues", NewQueueBody(location!.Id));

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<QueueBody>(response);
    Assert.NotEqual(Guid.Empty, body!.Id);
    Assert.Equal("Pescadería", body.Name);
    Assert.Equal("P", body.Code);
    Assert.Equal(5, body.Priority);
  }

  [Fact]
  public async Task Create_returns_401_without_authentication()
  {
    var client = Factory.CreateTestClient();

    var response = await client.PostAsync("/api/queues", new
    {
      name = "Pescadería",
      description = "",
      code = "P",
      maxValue = (int?)null,
      locationId = Guid.NewGuid(),
      counters = Array.Empty<Guid>(),
      priority = 5,
      weight = 2,
      agingIntervalMinutes = 10,
      maxAgingBonus = 3,
      color = "blue"
    });

    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task NewTicket_generates_sequential_codes_without_authentication()
  {
    // Escenario Keues-TicketMachine: se emiten tickets sin login.
    var client = Factory.CreateTestClient();
    var env = await SeedEnvironmentAsync();

    var first = await client.PostAsync($"/api/queues/{env.QueueId}/new-ticket", new { flowId = env.FlowId });
    var second = await client.PostAsync($"/api/queues/{env.QueueId}/new-ticket", new { flowId = env.FlowId });

    Assert.Equal(System.Net.HttpStatusCode.OK, first.StatusCode);
    var firstBody = await client.ReadAsync<TicketBody>(first);
    var secondBody = await client.ReadAsync<TicketBody>(second);
    Assert.Equal("P001", firstBody!.Code);
    Assert.Equal("P002", secondBody!.Code);
  }

  [Fact]
  public async Task NewTicket_unknown_queue_returns_400()
  {
    var client = Factory.CreateTestClient();

    var response = await client.PostAsync($"/api/queues/{Guid.NewGuid()}/new-ticket", new { flowId = Guid.NewGuid() });

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task NewTicket_with_non_guid_flow_in_body_returns_400()
  {
    var client = Factory.CreateTestClient();
    var env = await SeedEnvironmentAsync();

    var response = await client.PostAsync($"/api/queues/{env.QueueId}/new-ticket", new { flowId = "not-a-guid" });

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Get_returns_the_queue()
  {
    var client = await CreateAuthenticatedClientAsync();
    var env = await CreateEnvironmentAsync(client);

    var response = await client.GetAsync($"/api/queues/{env.QueueId}");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<QueueBody>(response);
    Assert.Equal(env.QueueId, body!.Id);
  }

  [Fact]
  public async Task Update_returns_the_updated_queue()
  {
    var client = await CreateAuthenticatedClientAsync();
    var env = await CreateEnvironmentAsync(client);

    var response = await client.PutAsync($"/api/queues/{env.QueueId}", new
    {
      name = "Pescadería nueva",
      description = "Nueva",
      code = "F",
      maxValue = (int?)null,
      locationId = env.LocationId,
      counters = Array.Empty<Guid>(),
      priority = 9,
      weight = 4,
      agingIntervalMinutes = 20,
      maxAgingBonus = 7,
      color = "red"
    });

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<QueueBody>(response);
    Assert.Equal("Pescadería nueva", body!.Name);
    Assert.Equal("F", body.Code);
    Assert.Equal(9, body.Priority);
  }

  [Fact]
  public async Task Update_returns_401_without_authentication()
  {
    var client = Factory.CreateTestClient();

    var response = await client.PutAsync($"/api/queues/{Guid.NewGuid()}", new
    {
      name = "X",
      description = "",
      code = "F",
      maxValue = (int?)null,
      locationId = Guid.NewGuid(),
      counters = Array.Empty<Guid>(),
      priority = 0,
      weight = 1,
      agingIntervalMinutes = 0,
      maxAgingBonus = 0,
      color = "blue"
    });

    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Delete_returns_200_and_hides_the_queue()
  {
    var client = await CreateAuthenticatedClientAsync();
    var env = await CreateEnvironmentAsync(client);

    var response = await client.DeleteAsync($"/api/queues/{env.QueueId}");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var get = await client.GetAsync($"/api/queues/{env.QueueId}");
    Assert.Equal(System.Net.HttpStatusCode.BadRequest, get.StatusCode);
  }

  [Fact]
  public async Task Delete_returns_401_without_authentication()
  {
    var client = Factory.CreateTestClient();

    var response = await client.DeleteAsync($"/api/queues/{Guid.NewGuid()}");

    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
  }

  private async Task<EnvironmentIds> CreateEnvironmentAsync(TestClient client)
  {
    var location = await client.ReadAsync<LocationBody>(
      await client.PostAsync("/api/locations", new { name = "Tienda", description = "", color = "blue" }));
    var flow = await client.ReadAsync<FlowBody>(
      await client.PostAsync("/api/flows", new
      {
        name = "Flujo principal",
        description = "",
        flowType = (int)FlowType.TicketMachine,
        locationId = location!.Id,
        flowJson = "{}"
      }));
    var queue = await client.ReadAsync<QueueBody>(
      await client.PostAsync("/api/queues", NewQueueBody(location.Id)));
    return new EnvironmentIds(location.Id, flow!.Id, queue!.Id);
  }

  private async Task<EnvironmentIds> SeedEnvironmentAsync()
  {
    var env = new EnvironmentIds(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    await Factory.WithContextAsync(async context =>
    {
      var location = await Seed.LocationAsync(context, "Tienda");
      var flow = await Seed.FlowAsync(context, location.Id);
      var queue = await Seed.QueueAsync(context, location.Id, code: "P");
      env = new EnvironmentIds(location.Id, flow.Id, queue.Id);
    });
    return env;
  }

  private static object NewQueueBody(Guid locationId) => new
  {
    name = "Pescadería",
    description = "Cola de pescadería",
    code = "P",
    maxValue = (int?)null,
    locationId,
    counters = Array.Empty<Guid>(),
    priority = 5,
    weight = 2,
    agingIntervalMinutes = 10,
    maxAgingBonus = 3,
    color = "blue"
  };

  private sealed record EnvironmentIds(Guid LocationId, Guid FlowId, Guid QueueId);
  private sealed record LocationBody(Guid Id, string Name, string? Description, string Color);
  private sealed record FlowBody(Guid Id, string Name, string Description, int FlowType);
  private sealed record QueueBody(Guid Id, string Name, string Description, int? MaxValue, string Code,
    int Priority, int Weight, int AgingIntervalMinutes, int MaxAgingBonus, string Color, List<Guid> Counters);
  private sealed record TicketBody(Guid Id, string Code);
}
