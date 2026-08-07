using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.Api;

public class CountersApiTests : ApiTestBase
{
  [Fact]
  public async Task Create_links_the_queues()
  {
    var client = await CreateAuthenticatedClientAsync();
    var env = await CreateEnvironmentAsync(client);

    var response = await client.PostAsync("/api/counters", new
    {
      name = "Caja 1",
      code = "C1",
      color = "green",
      description = "Caja principal",
      locationId = env.LocationId,
      queues = new[] { env.QueueId }
    });

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<CounterBody>(response);
    Assert.Equal("Caja 1", body!.Name);
    Assert.Equal(new[] { env.QueueId }, body.Queues);
  }

  [Fact]
  public async Task Create_returns_401_without_authentication()
  {
    var client = Factory.CreateTestClient();

    var response = await client.PostAsync("/api/counters", new
    {
      name = "Caja 1",
      code = "C1",
      color = "green",
      description = "",
      locationId = Guid.NewGuid(),
      queues = Array.Empty<Guid>()
    });

    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task CallNextTicket_calls_and_recalls_the_ticket_without_authentication()
  {
    // Escenario Keues-Counter: operaciones sin login.
    var client = Factory.CreateTestClient();
    var env = await SeedEnvironmentAsync();
    await client.PostAsync($"/api/queues/{env.QueueId}/new-ticket", new { flowId = env.FlowId });
    await client.PostAsync($"/api/queues/{env.QueueId}/new-ticket", new { flowId = env.FlowId });

    var first = await client.PostAsync($"/api/counters/{env.CounterId}/call-next-ticket", new { flowId = env.FlowId });
    var second = await client.PostAsync($"/api/counters/{env.CounterId}/call-next-ticket", new { flowId = env.FlowId });

    Assert.Equal(System.Net.HttpStatusCode.OK, first.StatusCode);
    var firstBody = await client.ReadAsync<CallNextBody>(first);
    var secondBody = await client.ReadAsync<CallNextBody>(second);
    Assert.Equal("P001", firstBody!.Code);
    Assert.Equal(firstBody.TicketId, secondBody!.TicketId);
  }

  [Fact]
  public async Task CallNextTicket_returns_null_when_no_waiting()
  {
    var client = Factory.CreateTestClient();
    var env = await SeedEnvironmentAsync();

    var response = await client.PostAsync($"/api/counters/{env.CounterId}/call-next-ticket", new { flowId = env.FlowId });

    // El controlador devuelve Json(null): 200 con body "null".
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("null", (await response.Content.ReadAsStringAsync()).Trim());
  }

  [Fact]
  public async Task AttendTicket_returns_200_without_authentication()
  {
    var client = Factory.CreateTestClient();
    var env = await SeedEnvironmentAsync();
    var ticket = await client.ReadAsync<TicketBody>(
      await client.PostAsync($"/api/queues/{env.QueueId}/new-ticket", new { flowId = env.FlowId }));
    await client.PostAsync($"/api/counters/{env.CounterId}/call-next-ticket", new { flowId = env.FlowId });

    var response = await client.PostAsync($"/api/counters/{env.CounterId}/attend-ticket", new
    {
      ticketId = ticket!.Id,
      flowId = env.FlowId
    });

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task CancelTicket_returns_200_without_authentication()
  {
    var client = Factory.CreateTestClient();
    var env = await SeedEnvironmentAsync();
    var ticket = await client.ReadAsync<TicketBody>(
      await client.PostAsync($"/api/queues/{env.QueueId}/new-ticket", new { flowId = env.FlowId }));

    var response = await client.PostAsync($"/api/counters/{env.CounterId}/cancel-ticket", new
    {
      ticketId = ticket!.Id
    });

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task SetFree_returns_200_without_authentication()
  {
    var client = Factory.CreateTestClient();
    var env = await SeedEnvironmentAsync();

    var response = await client.PostAsync($"/api/counters/{env.CounterId}/set-free", new { flowId = env.FlowId });

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task CallManualTicket_returns_200_without_authentication()
  {
    var client = Factory.CreateTestClient();
    var env = await SeedEnvironmentAsync();

    var response = await client.PostAsync($"/api/counters/{env.CounterId}/call-manual-ticket", new
    {
      code = "F-100",
      flowId = env.FlowId,
      locationId = env.LocationId,
      counterId = env.CounterId
    });

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task CallManualTicket_unknown_counter_returns_400()
  {
    var client = Factory.CreateTestClient();

    var response = await client.PostAsync($"/api/counters/{Guid.NewGuid()}/call-manual-ticket", new
    {
      code = "F-100",
      flowId = Guid.NewGuid(),
      locationId = Guid.NewGuid(),
      counterId = Guid.NewGuid()
    });

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task CallNextTicket_with_non_guid_counter_in_route_returns_404()
  {
    var client = Factory.CreateTestClient();

    var response = await client.PostAsync("/api/counters/not-a-guid/call-next-ticket", new { flowId = Guid.NewGuid() });

    Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task CallNextTicket_with_non_guid_flow_in_body_returns_400()
  {
    var client = Factory.CreateTestClient();
    var env = await SeedEnvironmentAsync();

    var response = await client.PostAsync($"/api/counters/{env.CounterId}/call-next-ticket", new { flowId = "not-a-guid" });

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
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
      await client.PostAsync("/api/queues", new
      {
        name = "Pescadería",
        description = "",
        code = "P",
        maxValue = (int?)null,
        locationId = location.Id,
        counters = Array.Empty<Guid>(),
        priority = 5,
        weight = 1,
        agingIntervalMinutes = 0,
        maxAgingBonus = 0,
        color = "blue"
      }));
    return new EnvironmentIds(location.Id, flow!.Id, queue!.Id, Guid.Empty);
  }

  private async Task<EnvironmentIds> SeedEnvironmentAsync()
  {
    var env = new EnvironmentIds(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    await Factory.WithContextAsync(async context =>
    {
      var location = await Seed.LocationAsync(context, "Tienda");
      var flow = await Seed.FlowAsync(context, location.Id);
      var queue = await Seed.QueueAsync(context, location.Id, code: "P");
      var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
      env = new EnvironmentIds(location.Id, flow.Id, queue.Id, counter.Id);
    });
    return env;
  }

  private sealed record EnvironmentIds(Guid LocationId, Guid FlowId, Guid QueueId, Guid CounterId);
  private sealed record LocationBody(Guid Id, string Name, string? Description, string Color);
  private sealed record FlowBody(Guid Id, string Name, string Description, int FlowType);
  private sealed record QueueBody(Guid Id, string Name, string Description, int? MaxValue, string Code,
    int Priority, int Weight, int AgingIntervalMinutes, int MaxAgingBonus, string Color, List<Guid> Counters);
  private sealed record CounterBody(Guid Id, string Name, string Code, string? Description, string? Color,
    IEnumerable<Guid> Queues, Guid LocationId);
  private sealed record TicketBody(Guid Id, string Code);
  private sealed record CallNextBody(Guid TicketId, string Code, Guid QueueId);
}
