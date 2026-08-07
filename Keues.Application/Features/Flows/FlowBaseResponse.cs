using Keues.Application.Features.Locations;
using Keues.Domain.Enums;

namespace Keues.Application.Features.Flows;

public record FlowBaseResponse(Guid Id, string Name, string Description, FlowType FlowType, string FlowJson, LocationBaseResponse Location,DateTime CreatedAt);