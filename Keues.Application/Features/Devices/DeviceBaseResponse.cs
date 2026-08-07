using Keues.Domain.Enums;

namespace Keues.Application.Features.Devices;

public record DeviceBaseResponse(Guid Id, string Name, DeviceType Type, DateTime LastConnection);