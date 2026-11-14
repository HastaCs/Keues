using System.Diagnostics;
using Keues.Application.DeviceRegistry;
using Keues.Application.Features.Devices;
using Keues.Application.Features.Qr;
using Keues.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace Keues.API.Hubs;

public class DeviceHub : Hub
{
  private readonly ConnectedDeviceRegistry _registry;
  private readonly DeviceUseCases _deviceUseCases;
  private readonly QrTokenPayloadFactory _qrTokenPayloadFactory;

  public DeviceHub(
    ConnectedDeviceRegistry registry,
    DeviceUseCases deviceUseCases,
    QrTokenPayloadFactory qrTokenPayloadFactory)
  {
    _registry = registry;
    _deviceUseCases = deviceUseCases;
    _qrTokenPayloadFactory = qrTokenPayloadFactory;
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

    _registry.Add(new RegistryDevice(device.Id, device.Type, Context.ConnectionId));

    //Por si hay mas de un device, por ejempo varios monitores en la misma ubicacion
    await Groups.AddToGroupAsync(Context.ConnectionId, DeviceGroups.Build(locationId, typeDevice, flowId));
    
    await _deviceUseCases.CreateUpdate.Handle(device);

    if (typeDevice == DeviceType.TicketMachine)
    {
      var payload = await _qrTokenPayloadFactory.BuildAsync(flowId, DateTimeOffset.UtcNow);
      if (payload is not null)
      {
        await Clients.Caller.SendAsync("QrToken", payload);
      }
    }

    await base.OnConnectedAsync();
  }


  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    _registry.Remove(Context.ConnectionId);
    Debug.WriteLine($"Device disconnected: {Context.ConnectionId}");
    await base.OnDisconnectedAsync(exception);
  }
}