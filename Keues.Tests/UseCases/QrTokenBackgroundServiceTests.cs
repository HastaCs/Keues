using Keues.API.BackgroundServices;
using Keues.API.Hubs;
using Xunit;

namespace Keues.Tests.UseCases;

public class QrTokenBackgroundServiceTests
{
  [Fact]
  public void GetDelayUntilNextRotation_at_a_boundary_returns_five_minutes_and_margin()
  {
    var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    var delay = QrTokenBackgroundService.GetDelayUntilNextRotation(now, 300);

    Assert.Equal(TimeSpan.FromMinutes(5) + TimeSpan.FromMilliseconds(500), delay);
  }

  [Fact]
  public void GetDelayUntilNextRotation_just_before_a_boundary_returns_the_remaining_time()
  {
    var now = new DateTimeOffset(2026, 1, 1, 12, 4, 59, TimeSpan.Zero);

    var delay = QrTokenBackgroundService.GetDelayUntilNextRotation(now, 300);

    Assert.Equal(TimeSpan.FromSeconds(1) + TimeSpan.FromMilliseconds(500), delay);
  }

  [Fact]
  public void GetDelayUntilNextRotation_at_the_next_boundary_returns_five_minutes_and_margin()
  {
    var now = new DateTimeOffset(2026, 1, 1, 12, 5, 0, TimeSpan.Zero);

    var delay = QrTokenBackgroundService.GetDelayUntilNextRotation(now, 300);

    Assert.Equal(TimeSpan.FromMinutes(5) + TimeSpan.FromMilliseconds(500), delay);
  }

  [Fact]
  public void GetDelayUntilNextRotation_rejects_an_invalid_slot()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() =>
      QrTokenBackgroundService.GetDelayUntilNextRotation(DateTimeOffset.UtcNow, 0));
  }

  [Fact]
  public void TicketMachine_group_is_scoped_by_location_and_flow()
  {
    var locationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var flowId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    var group = DeviceGroups.TicketMachine(locationId, flowId);

    Assert.Equal($"locationId:{locationId}:typeDevice:TicketMachine:flowId:{flowId}", group);
  }
}
