using Keues.Application.Features.Devices;
using Keues.Domain.Enums;

namespace Keues.API.Responses.Devices;

public record DeviceResponse(
 Guid Id,
 string Name,
 DeviceType Type,
 DateTime LastConnection,
 bool IsConnected
) : DeviceBaseResponse(Id, Name, Type, LastConnection);