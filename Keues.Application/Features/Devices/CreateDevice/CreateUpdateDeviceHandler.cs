using Keues.Application.Common;
using Keues.Domain.Entities;

namespace Keues.Application.Features.Devices.CreateDevice;

public class CreateUpdateDeviceHandler
{
  private readonly IApplicationDbContext _context;

  public CreateUpdateDeviceHandler(IApplicationDbContext context)
  {
    _context = context;
  }
//TODO EL remove device
  public async Task<DeviceBaseResponse> Handle(DeviceBaseCommand command)
  {
    var device = await _context.Devices.FindAsync(command.Id) ?? new Device();
    device.Name = command.Name;
    device.Type = command.Type;
    device.LocationId = command.LocationId;
    device.LastConnection = DateTime.UtcNow;
    if (device.Id == Guid.Empty)
    {
      device.Id = command.Id;
      device.CreatedAt = DateTime.UtcNow;
      await _context.Devices.AddAsync(device);
    }
    await _context.SaveChangesAsync();
    return new DeviceBaseResponse(device.Id, device.Name, device.Type,device.LastConnection);
  }
}
  
   