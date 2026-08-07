using Keues.Domain.Enums;

namespace Keues.Application.Features.Devices.GetDevices;

public record GetDevicesCommand(Guid? LocationId,DeviceType? DeviceType);
