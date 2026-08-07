

using Keues.Application.Features.Flows.CreateFlow;
using Keues.Application.Features.Flows.DeleteFlow;
using Keues.Application.Features.Flows.GetAllFlows;
using Keues.Application.Features.Flows.GetFlow;
using Keues.Application.Features.Flows.UpdateFlow;

namespace Keues.Application.Features.Flows;

public class FlowsUseCases( CreateFlowHandler create, UpdateFlowHandler update, DeleteFlowHandler delete, GetFlowHandler get, GetAllFlowsHandler getAll)
{
  public CreateFlowHandler Create { get; } = create;
  public UpdateFlowHandler Update { get; } = update;
  public DeleteFlowHandler Delete { get; } = delete;
  public GetFlowHandler Get { get; } = get;
  public GetAllFlowsHandler GetAll { get; } = getAll;
}
