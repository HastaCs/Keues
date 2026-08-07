using Keues.Application.Features.Devices.CreateDevice;
using Keues.Application.Features.Devices.DeleteDevice;
using Keues.Application.Features.Devices.GetDevices;

namespace Keues.Application.Features.Devices;

public class DeviceUseCases(
  CreateUpdateDeviceHandler createUpdate,
  GetDevicesHandler getDevices,
  DeleteDeviceHandler deleteDevice)
{
  public CreateUpdateDeviceHandler CreateUpdate { get; } = createUpdate;
  public GetDevicesHandler GetDevices { get; } = getDevices;
  public DeleteDeviceHandler Delete { get; } = deleteDevice;
}