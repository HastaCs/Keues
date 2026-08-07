using Keues.Application.Common;
using Keues.Application.DeviceRegistry;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Devices.DeleteDevice;

public class DeleteDeviceHandler
{
  private readonly IApplicationDbContext _context;
  private readonly ConnectedDeviceRegistry _devicesRegistry;

  public DeleteDeviceHandler(IApplicationDbContext context, ConnectedDeviceRegistry devicesRegistry)
  {
    _context = context;
    _devicesRegistry = devicesRegistry;
  }

  public async Task Handle(DeleteDeviceCommand command)
  {
    var device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == command.Id);
    if (device == null)
      throw new Exception("Device not found");

    if (_devicesRegistry.IsConnected(command.Id))
      throw new Exception("Cannot delete a connected device");

    _context.Devices.Remove(device);
    await _context.SaveChangesAsync();
  }
}