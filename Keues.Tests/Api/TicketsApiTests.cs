using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.Api;

public class TicketsApiTests : ApiTestBase
{
  private async Task<EnvironmentIds> CreateEnvironmentAsync()
  {
    var env = new EnvironmentIds(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    await Factory.WithContextAsync(async context =>
    {
      var location = await Seed.LocationAsync(context, "Tienda");
      var flow = await Seed.FlowAsync(context, location.Id);
      var queue = await Seed.QueueAsync(context, location.Id, code: "P", name: "Pescadería");
      var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
      await Seed.TicketAsync(context, queue.Id, flow.Id);
      await Seed.TicketAsync(context, queue.Id, flow.Id, status: TicketStatus.Canceled);
      env = new EnvironmentIds(location.Id, flow.Id, queue.Id, counter.Id);
    });
    return env;
  }

  [Fact]
  public async Task GetAll_returns_empty_without_tickets()
  {
    var client = Factory.CreateTestClient();

    var response = await client.GetAsync("/api/tickets");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<DataBody<TicketBody>>(response);
    Assert.Empty(body!.Data);
  }

  [Fact]
  public async Task GetAll_filters_by_status_and_location()
  {
    var client = Factory.CreateTestClient();
    var env = await CreateEnvironmentAsync();

    var response = await client.GetAsync(
      $"/api/tickets?status=Waiting&locationId={env.LocationId}");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<DataBody<TicketBody>>(response);
    var ticket = Assert.Single(body!.Data);
    Assert.Equal((int)TicketStatus.Waiting, ticket.Status);
    Assert.Equal("P001", ticket.Code);
    Assert.Equal("Pescadería", ticket.Queue!.Name);
  }

  [Fact]
  public async Task Get_returns_the_ticket()
  {
    var client = Factory.CreateTestClient();
    var env = await CreateEnvironmentAsync();
    var all = await client.ReadAsync<DataBody<TicketBody>>(await client.GetAsync("/api/tickets"));
    var ticketId = all!.Data.First().Id;

    var response = await client.GetAsync($"/api/tickets/{ticketId}");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<TicketBody>(response);
    Assert.Equal(ticketId, body!.Id);
  }

  [Fact]
  public async Task Get_unknown_ticket_returns_400()
  {
    var client = Factory.CreateTestClient();

    var response = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}");

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task GetAll_with_non_guid_location_query_returns_400()
  {
    var client = Factory.CreateTestClient();

    var response = await client.GetAsync("/api/tickets?locationId=not-a-guid");

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task GetAll_with_invalid_status_query_returns_400()
  {
    var client = Factory.CreateTestClient();

    var response = await client.GetAsync("/api/tickets?status=NotAStatus");

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  private sealed record EnvironmentIds(Guid LocationId, Guid FlowId, Guid QueueId, Guid CounterId);
  private sealed record TicketBody(Guid Id, string Code, int Status, QueueMinBody? Queue);
  private sealed record QueueMinBody(Guid Id, string Name);
  private sealed record DataBody<T>(List<T> Data);
}
