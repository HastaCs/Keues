namespace Keues.Application.Features.Devices.IsActiveDevice;

public record IsActiveCommand(bool IsActive,Guid DeviceId);