using Keues.Application.DeviceRegistry;
using Keues.Domain.Entities;
using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keues.Tests.Api;

public class DevicesApiTests : ApiTestBase
{
  private async Task<Guid> CreateLocationAsync(TestClient client)
  {
    var location = await client.ReadAsync<LocationBody>(
      await client.PostAsync("/api/locations", new { name = "Tienda", description = "", color = "blue" }));
    return location!.Id;
  }

  private async Task SeedDeviceAsync(Guid id, Guid locationId, DeviceType type)
  {
    await Factory.WithContextAsync(async context =>
    {
      context.Devices.Add(new Device
      {
        Id = id,
        Name = $"Device {id}",
        Type = type,
        LocationId = locationId,
        LastConnection = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow
      });
      await context.SaveChangesAsync();
    });
  }

  [Fact]
  public async Task GetDevices_returns_empty_when_there_are_no_devices()
  {
    var client = await CreateAuthenticatedClientAsync();

    var response = await client.GetAsync("/api/devices");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<DataBody<DeviceBody>>(response);
    Assert.Empty(body!.Data);
  }

  [Fact]
  public async Task GetDevices_returns_connection_status()
  {
    var client = await CreateAuthenticatedClientAsync();
    var locationId = await CreateLocationAsync(client);
    var deviceId = Guid.NewGuid();
    await SeedDeviceAsync(deviceId, locationId, DeviceType.Monitor);

    var response = await client.GetAsync($"/api/devices?locationId={locationId}&deviceType=Monitor");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<DataBody<DeviceBody>>(response);
    var device = Assert.Single(body!.Data);
    Assert.Equal(deviceId, device.Id);
    Assert.False(device.IsConnected);

    // Al registrarse en el registry (simulando un SignalR conectado) aparece como conectado.
    var registry = Factory.Services.GetRequiredService<ConnectedDeviceRegistry>();
    registry.Add(new RegistryDevice(deviceId, DeviceType.Monitor, "conn-1"));

    var again = await client.ReadAsync<DataBody<DeviceBody>>(
      await client.GetAsync($"/api/devices?locationId={locationId}&deviceType=Monitor"));
    Assert.True(again!.Data.Single().IsConnected);
  }

  [Fact]
  public async Task Delete_connected_device_returns_400()
  {
    var client = await CreateAuthenticatedClientAsync();
    var locationId = await CreateLocationAsync(client);
    var deviceId = Guid.NewGuid();
    await SeedDeviceAsync(deviceId, locationId, DeviceType.Monitor);

    var registry = Factory.Services.GetRequiredService<ConnectedDeviceRegistry>();
    registry.Add(new RegistryDevice(deviceId, DeviceType.Monitor, "conn-1"));

    var response = await client.DeleteAsync($"/api/devices/{deviceId}");

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Delete_disconnected_device_returns_200()
  {
    var client = await CreateAuthenticatedClientAsync();
    var locationId = await CreateLocationAsync(client);
    var deviceId = Guid.NewGuid();
    await SeedDeviceAsync(deviceId, locationId, DeviceType.Monitor);

    var response = await client.DeleteAsync($"/api/devices/{deviceId}");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var get = await client.ReadAsync<DataBody<DeviceBody>>(await client.GetAsync("/api/devices"));
    Assert.Empty(get!.Data);
  }

  [Fact]
  public async Task Delete_unknown_device_returns_400()
  {
    var client = await CreateAuthenticatedClientAsync();

    var response = await client.DeleteAsync($"/api/devices/{Guid.NewGuid()}");

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Delete_returns_401_without_authentication()
  {
    var client = Factory.CreateTestClient();

    var response = await client.DeleteAsync($"/api/devices/{Guid.NewGuid()}");

    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
  }

  private sealed record LocationBody(Guid Id, string Name, string? Description, string Color);
  private sealed record DeviceBody(Guid Id, string Name, int Type, DateTime LastConnection, bool IsConnected);
  private sealed record DataBody<T>(List<T> Data);
}
