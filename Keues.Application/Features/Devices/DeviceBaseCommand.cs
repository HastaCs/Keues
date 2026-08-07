using Keues.Domain.Enums;

namespace Keues.Application.Features.Devices;

public record DeviceBaseCommand(Guid Id, string Name, Guid LocationId, DeviceType Type);
