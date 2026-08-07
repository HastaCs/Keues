namespace Keues.Application.Features.Flows.UpdateFlow;

public record UpdateFlowCommand(Guid Id, string Name, string Description,string FlowJson);