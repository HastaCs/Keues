using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Devices.GetDevices;

public class GetDevicesHandler
{
  private readonly IApplicationDbContext _context;

  public GetDevicesHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<IEnumerable<DeviceBaseResponse>> Handle(GetDevicesCommand command)
  {
    var devices = _context.Devices.AsQueryable();

    if (command.LocationId.HasValue)
    {
      devices = devices.Where(d => d.LocationId == command.LocationId.Value);
    }

    if (command.DeviceType.HasValue)
    {
      devices = devices.Where(d => d.Type == command.DeviceType.Value);
    }

    var deviceList = await devices.ToListAsync();

    return deviceList.Select(d => new DeviceBaseResponse(d.Id, d.Name, d.Type,d.LastConnection));
  }
}
