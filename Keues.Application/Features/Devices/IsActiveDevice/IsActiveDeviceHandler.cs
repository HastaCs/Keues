using Keues.Application.Common;
using Keues.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Devices.IsActiveDevice;

public class IsActiveDeviceHandler
{
  private readonly IApplicationDbContext _context;

  public IsActiveDeviceHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  public async Task Handle(IsActiveCommand command)
  {
    var device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == command.DeviceId && d.Type==DeviceType.TicketMachine);
    if (device == null)
    {
      throw new Exception("Device not found");
    }
    device.IsActive = command.IsActive;
    await _context.SaveChangesAsync();
  }
}