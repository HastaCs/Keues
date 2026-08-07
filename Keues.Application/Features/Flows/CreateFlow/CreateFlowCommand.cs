using Keues.Domain.Enums;

namespace Keues.Application.Features.Flows.CreateFlow;

public record CreateFlowCommand(string Name,string Description, FlowType FlowType ,Guid LocationId,string FlowJson);