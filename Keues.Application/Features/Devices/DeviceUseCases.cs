using Keues.Application.Features.Devices.CreateDevice;
using Keues.Application.Features.Devices.DeleteDevice;
using Keues.Application.Features.Devices.GetDevice;
using Keues.Application.Features.Devices.GetDevices;
using Keues.Application.Features.Devices.IsActiveDevice;

namespace Keues.Application.Features.Devices;

public class DeviceUseCases(
  CreateUpdateDeviceHandler createUpdate,
  GetDevicesHandler getDevices,
  DeleteDeviceHandler deleteDevice,
  IsActiveDeviceHandler isActiveDeviceHandler,
  GetDeviceHandler getDeviceHandler)
{
  public CreateUpdateDeviceHandler CreateUpdate { get; } = createUpdate;
  public GetDevicesHandler GetDevices { get; } = getDevices;
  public DeleteDeviceHandler Delete { get; } = deleteDevice;
  public IsActiveDeviceHandler IsActive { get; } = isActiveDeviceHandler;
  public GetDeviceHandler GetDevice { get; } = getDeviceHandler;
}