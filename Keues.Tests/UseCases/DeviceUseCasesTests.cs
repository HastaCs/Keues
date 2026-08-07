using Keues.Application.DeviceRegistry;
using Keues.Application.Features.Devices;
using Keues.Application.Features.Devices.CreateDevice;
using Keues.Application.Features.Devices.DeleteDevice;
using Keues.Application.Features.Devices.GetDevices;
using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.UseCases;

public class DeviceUseCasesTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  [Fact]
  public async Task CreateUpdate_creates_a_new_device()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var deviceId = Guid.NewGuid();
    var handler = new CreateUpdateDeviceHandler(context);

    var response = await handler.Handle(new DeviceBaseCommand(deviceId, "Monitor 1", location.Id, DeviceType.Monitor));

    Assert.Equal(deviceId, response.Id);
    Assert.Equal("Monitor 1", response.Name);
    Assert.Equal(DeviceType.Monitor, response.Type);
    Assert.NotNull(await context.Devices.FindAsync(deviceId));
  }

  [Fact]
  public async Task CreateUpdate_updates_an_existing_device()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var deviceId = Guid.NewGuid();
    var handler = new CreateUpdateDeviceHandler(context);
    await handler.Handle(new DeviceBaseCommand(deviceId, "Monitor 1", location.Id, DeviceType.Monitor));

    var response = await handler.Handle(new DeviceBaseCommand(deviceId, "Monitor renombrado", location.Id, DeviceType.Monitor));

    Assert.Equal("Monitor renombrado", response.Name);
    Assert.Single(context.Devices);
  }

  [Fact]
  public async Task GetDevices_filters_by_location_and_type()
  {
    await using var context = _db.CreateContext();
    var locA = await Seed.LocationAsync(context, "A");
    var locB = await Seed.LocationAsync(context, "B");
    var handler = new CreateUpdateDeviceHandler(context);
    await handler.Handle(new DeviceBaseCommand(Guid.NewGuid(), "M1", locA.Id, DeviceType.Monitor));
    await handler.Handle(new DeviceBaseCommand(Guid.NewGuid(), "C1", locA.Id, DeviceType.Counter));
    await handler.Handle(new DeviceBaseCommand(Guid.NewGuid(), "M2", locB.Id, DeviceType.Monitor));
    var getHandler = new GetDevicesHandler(context);

    var monitorsA = await getHandler.Handle(new GetDevicesCommand(locA.Id, DeviceType.Monitor));
    var allA = await getHandler.Handle(new GetDevicesCommand(locA.Id, null));
    var all = await getHandler.Handle(new GetDevicesCommand(null, null));

    Assert.Single(monitorsA);
    Assert.Equal(2, allA.Count());
    Assert.Equal(3, all.Count());
  }

  [Fact]
  public async Task Delete_deletes_a_device_that_is_not_connected()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var deviceId = Guid.NewGuid();
    var createHandler = new CreateUpdateDeviceHandler(context);
    await createHandler.Handle(new DeviceBaseCommand(deviceId, "Monitor", location.Id, DeviceType.Monitor));
    var deleteHandler = new DeleteDeviceHandler(context, new ConnectedDeviceRegistry());

    await deleteHandler.Handle(new DeleteDeviceCommand(deviceId));

    Assert.Null(await context.Devices.FindAsync(deviceId));
  }

  [Fact]
  public async Task Delete_throws_when_device_is_connected()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var deviceId = Guid.NewGuid();
    var createHandler = new CreateUpdateDeviceHandler(context);
    await createHandler.Handle(new DeviceBaseCommand(deviceId, "Monitor", location.Id, DeviceType.Monitor));

    var registry = new ConnectedDeviceRegistry();
    registry.Add(new RegistryDevice(deviceId, DeviceType.Monitor, "conn-1"));
    var deleteHandler = new DeleteDeviceHandler(context, registry);

    await Assert.ThrowsAsync<Exception>(() =>
      deleteHandler.Handle(new DeleteDeviceCommand(deviceId)));
  }

  [Fact]
  public async Task Delete_throws_when_device_not_found()
  {
    await using var context = _db.CreateContext();
    var deleteHandler = new DeleteDeviceHandler(context, new ConnectedDeviceRegistry());

    await Assert.ThrowsAsync<Exception>(() =>
      deleteHandler.Handle(new DeleteDeviceCommand(Guid.NewGuid())));
  }
}
