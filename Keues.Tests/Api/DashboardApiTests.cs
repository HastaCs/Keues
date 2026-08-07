using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.Api;

public class DashboardApiTests : ApiTestBase
{
  [Fact]
  public async Task Get_returns_the_summary()
  {
    var client = Factory.CreateTestClient();
    var env = new EnvironmentIds(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    await Factory.WithContextAsync(async context =>
    {
      var location = await Seed.LocationAsync(context, "Tienda");
      var flow = await Seed.FlowAsync(context, location.Id, FlowType.TicketMachine);
      var queue = await Seed.QueueAsync(context, location.Id);
      var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
      await Seed.TicketAsync(context, queue.Id, flow.Id);
      var inProgress = await Seed.TicketAsync(context, queue.Id, flow.Id);
      inProgress.Status = TicketStatus.InProgress;
      inProgress.CounterId = counter.Id;
      inProgress.CalledAt = DateTime.UtcNow;
      await context.SaveChangesAsync();
      env = new EnvironmentIds(location.Id, flow.Id, queue.Id, counter.Id);
    });

    var response = await client.GetAsync($"/api/dashboard?locationId={env.LocationId}");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<DashboardBody>(response);
    Assert.Equal("Tienda", body!.Location.Name);
    Assert.Equal(1, body.Counters);
    Assert.Equal(1, body.Queues);
    Assert.Equal(2, body.TicketsToday.Total);
    Assert.Equal(1, body.TicketsToday.Waiting);
    Assert.Equal(1, body.TicketsToday.InProgress);
    var nowServing = Assert.Single(body.NowServing);
    Assert.Equal(env.CounterId, nowServing.CounterId);
  }

  [Fact]
  public async Task Get_unknown_location_returns_400()
  {
    var client = Factory.CreateTestClient();

    var response = await client.GetAsync($"/api/dashboard?locationId={Guid.NewGuid()}");

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Get_with_non_guid_location_returns_400()
  {
    var client = Factory.CreateTestClient();

    var response = await client.GetAsync("/api/dashboard?locationId=not-a-guid");

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  private sealed record EnvironmentIds(Guid LocationId, Guid FlowId, Guid QueueId, Guid CounterId);
  private sealed record LocationSummaryBody(Guid Id, string Name, string? Description, string Color);
  private sealed record TicketsTodayBody(int Total, int Waiting, int InProgress, int Attended, int Canceled);
  private sealed record NowServingBody(Guid CounterId, string CounterCode, Guid TicketId);
  private sealed record DashboardBody(
    LocationSummaryBody Location,
    int Counters,
    int Queues,
    TicketsTodayBody TicketsToday,
    double? AverageWaitMinutes,
    double? AverageServiceMinutes,
    List<NowServingBody> NowServing,
    List<object> WaitingTickets);
}
