using Keues.Application.Common;

namespace Keues.Application.Features.Devices.GetDevice;

public class GetDeviceHandler
{
    private readonly IApplicationDbContext _context;
    
    public GetDeviceHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<DeviceBaseResponse?> Handle(GetDeviceQuery query)
    {
        var device = await _context.Devices.FindAsync(query.DeviceId);
    
        if (device == null)
        return null;
    
        return new DeviceBaseResponse(device.Id, device.Name, device.Type, device.LastConnection, device.IsActive);
    }
}