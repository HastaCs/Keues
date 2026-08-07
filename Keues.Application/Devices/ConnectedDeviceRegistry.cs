

using Keues.Application.Features.Devices;
using Keues.Domain.Enums;

namespace Keues.Application.DeviceRegistry;

public record RegistryDevice(Guid Id, DeviceType DeviceType, string ConnectionId);
public class ConnectedDeviceRegistry
{
  
  private readonly List<RegistryDevice> _devices = new();

  public bool IsConnected(Guid deviceId)
  {
    return _devices.Any(x => x.Id == deviceId);
  }
  
  public void Add(RegistryDevice device)
  {
    _devices.RemoveAll(x => x.Id == device.Id);    
    _devices.Add(device);
  }


  public void Remove(string connectionId)
  {
    var device = _devices
      .FirstOrDefault(x => x.ConnectionId == connectionId);

    if (device != null)   
      _devices.Remove(device);
  }


  public IEnumerable<RegistryDevice> GetAll()
  {
    return _devices;
  }
}