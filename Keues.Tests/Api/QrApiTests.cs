using Keues.Application.Common;
using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keues.Tests.Api;

public class QrApiTests : ApiTestBase
{
  [Fact]
  public async Task CreateTicket_with_a_valid_token_creates_a_ticket_without_authentication()
  {
    var admin = await CreateAuthenticatedClientAsync();
    var environment = await SeedQrEnvironmentAsync(admin);
    var token = CreateToken(environment.FlowId);
    var client = Factory.CreateTestClient();

    var response = await PostQrTicketAsync(client, environment.QueueId, environment.FlowId, token);

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<TicketBody>(response);
    Assert.NotEqual(Guid.Empty, body!.Id);
    Assert.Equal("Q001", body.Code);
  }

  [Fact]
  public async Task CreateTicket_rejects_an_invalid_token()
  {
    var admin = await CreateAuthenticatedClientAsync();
    var environment = await SeedQrEnvironmentAsync(admin);
    var client = Factory.CreateTestClient();

    var response = await PostQrTicketAsync(
      client,
      environment.QueueId,
      environment.FlowId,
      "not-a-token");

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task CreateTicket_rejects_an_expired_token()
  {
    var admin = await CreateAuthenticatedClientAsync();
    var environment = await SeedQrEnvironmentAsync(admin);
    var token = CreateToken(environment.FlowId, DateTimeOffset.UtcNow.AddMinutes(-11));
    var client = Factory.CreateTestClient();

    var response = await PostQrTicketAsync(client, environment.QueueId, environment.FlowId, token);

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task CreateTicket_rejects_a_token_from_another_flow()
  {
    var admin = await CreateAuthenticatedClientAsync();
    var environment = await SeedQrEnvironmentAsync(admin);
    var otherFlow = await CreateQrFlowAsync(
      admin,
      environment.LocationId,
      await CreateQueueAsync(admin, environment.LocationId, code: "R"));
    var token = CreateToken(otherFlow.Id);
    var client = Factory.CreateTestClient();

    var response = await PostQrTicketAsync(client, environment.QueueId, environment.FlowId, token);

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task CreateTicket_rejects_a_queue_that_does_not_belong_to_the_flow()
  {
    var admin = await CreateAuthenticatedClientAsync();
    var environment = await SeedQrEnvironmentAsync(admin);
    var otherQueue = await CreateQueueAsync(admin, environment.LocationId, code: "R");
    var token = CreateToken(environment.FlowId);
    var client = Factory.CreateTestClient();

    var response = await PostQrTicketAsync(client, otherQueue.Id, environment.FlowId, token);

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task CreateTicket_rejects_a_non_qr_flow()
  {
    var admin = await CreateAuthenticatedClientAsync();
    var location = await CreateLocationAsync(admin);
    var queue = await CreateQueueAsync(admin, location.Id);
    var flow = await admin.ReadAsync<FlowBody>(
      await admin.PostAsync("/api/flows", new
      {
        name = "Ticket machine",
        description = "",
        flowType = (int)FlowType.TicketMachine,
        locationId = location.Id,
        flowJson = FlowJsonWithQueue(queue.Id)
      }));
    var token = CreateToken(flow!.Id);
    var client = Factory.CreateTestClient();

    var response = await PostQrTicketAsync(client, queue.Id, flow.Id, token);

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task CreateTicket_rejects_a_qr_flow_without_a_queue()
  {
    var admin = await CreateAuthenticatedClientAsync();
    var location = await CreateLocationAsync(admin);
    Guid flowId = Guid.Empty;

    await Factory.WithContextAsync(async context =>
    {
      var flow = await Seed.FlowAsync(context, location.Id, FlowType.QrVirtual, "QR sin cola");
      flow.FlowJson = "[]";
      await context.SaveChangesAsync();
      flowId = flow.Id;
    });

    var token = CreateToken(flowId);
    var client = Factory.CreateTestClient();

    var response = await PostQrTicketAsync(client, Guid.NewGuid(), flowId, token);

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task CreateTicket_generates_sequential_codes()
  {
    var admin = await CreateAuthenticatedClientAsync();
    var environment = await SeedQrEnvironmentAsync(admin);
    var token = CreateToken(environment.FlowId);
    var client = Factory.CreateTestClient();

    var first = await PostQrTicketAsync(client, environment.QueueId, environment.FlowId, token);
    var second = await PostQrTicketAsync(client, environment.QueueId, environment.FlowId, token);

    Assert.Equal(System.Net.HttpStatusCode.OK, first.StatusCode);
    Assert.Equal(System.Net.HttpStatusCode.OK, second.StatusCode);
    Assert.Equal("Q001", (await client.ReadAsync<TicketBody>(first))!.Code);
    Assert.Equal("Q002", (await client.ReadAsync<TicketBody>(second))!.Code);
  }

  private static Task<HttpResponseMessage> PostQrTicketAsync(
    TestClient client,
    Guid queueId,
    Guid flowId,
    string token)
  {
    return client.PostAsync($"/api/queues/{queueId}/qr-new-ticket", new
    {
      flowId,
      token
    });
  }

  private string CreateToken(Guid flowId, DateTimeOffset? now = null)
  {
    var tokenService = Factory.Services.GetRequiredService<IQrTokenService>();
    return tokenService.Create(flowId, now ?? DateTimeOffset.UtcNow);
  }

  private static async Task<QrEnvironment> SeedQrEnvironmentAsync(TestClient admin)
  {
    var location = await CreateLocationAsync(admin);
    var queue = await CreateQueueAsync(admin, location.Id);
    var flow = await CreateQrFlowAsync(admin, location.Id, queue);

    return new QrEnvironment(location.Id, queue.Id, flow.Id);
  }

  private static async Task<LocationBody> CreateLocationAsync(TestClient client)
  {
    return (await client.ReadAsync<LocationBody>(
      await client.PostAsync("/api/locations", new { name = "Tienda", description = "", color = "blue" })))!;
  }

  private static async Task<QueueBody> CreateQueueAsync(TestClient client, Guid locationId, string code = "Q")
  {
    var response = await client.PostAsync("/api/queues", new
    {
      name = "Cola QR",
      description = "",
      code,
      maxValue = (int?)null,
      locationId,
      counters = Array.Empty<Guid>(),
      priority = 0,
      weight = 1,
      agingIntervalMinutes = 0,
      maxAgingBonus = 0,
      color = "blue"
    });

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    return (await client.ReadAsync<QueueBody>(response))!;
  }

  private static async Task<FlowBody> CreateQrFlowAsync(TestClient client, Guid locationId, QueueBody queue)
  {
    var response = await client.PostAsync("/api/flows", new
    {
      name = "QR virtual",
      description = "",
      flowType = (int)FlowType.QrVirtual,
      locationId,
      flowJson = FlowJsonWithQueue(queue.Id)
    });

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    return (await client.ReadAsync<FlowBody>(response))!;
  }

  private static string FlowJsonWithQueue(Guid queueId) => $$"""
    [
      {
        "nodeType": "ticket",
        "queueId": "{{queueId}}"
      }
    ]
    """;

  private sealed record QrEnvironment(Guid LocationId, Guid QueueId, Guid FlowId);
  private sealed record LocationBody(Guid Id);
  private sealed record QueueBody(Guid Id);
  private sealed record FlowBody(Guid Id);
  private sealed record TicketBody(Guid Id, string Code);
}
