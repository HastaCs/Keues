using System.Diagnostics;
using Keues.Application.DeviceRegistry;
using Keues.Application.Features.Devices;
using Keues.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace Keues.API.Hubs;

public class DeviceHub : Hub
{
  private readonly ConnectedDeviceRegistry _registry;
  private readonly DeviceUseCases _deviceUseCases;

  public DeviceHub(ConnectedDeviceRegistry registry, DeviceUseCases deviceUseCases)
  {
    _registry = registry;
    _deviceUseCases = deviceUseCases;
  }

  public override async Task OnConnectedAsync()
  {
    var http = Context.GetHttpContext();
    Debug.WriteLine($"Device connected: {Context.ConnectionId}");
    Debug.WriteLine($"Query parameters: {http.Request.QueryString}");
    var id = Guid.Parse(http.Request.Query["deviceId"]);
    var name = http.Request.Query["name"];
    var locationId = Guid.Parse(http.Request.Query["locationId"]);
    var typeDevice = Enum.Parse<DeviceType>(http.Request.Query["type"]);
    var flowId = Guid.Parse(http.Request.Query["flowId"]);
    
    var device = new DeviceBaseCommand(id, name, locationId, typeDevice);
//Añado el device al registro, al grupo de signalr y a la base de datos

    _registry.Add(new RegistryDevice(device.Id, device.Type, Context.ConnectionId));
    
    await Groups.AddToGroupAsync(Context.ConnectionId, $"locationId:{locationId}:typeDevice:{typeDevice}:flowId:{flowId}");
    
    await _deviceUseCases.CreateUpdate.Handle(device);
    await base.OnConnectedAsync();
  }


  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    _registry.Remove(Context.ConnectionId);
    Debug.WriteLine($"Device disconnected: {Context.ConnectionId}");
    await base.OnDisconnectedAsync(exception);
  }
}