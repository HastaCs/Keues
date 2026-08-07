
using Keues.Application.Features.Flows.CreateFlow;
using Keues.Application.Features.Flows.DeleteFlow;
using Keues.Application.Features.Flows.GetAllFlows;
using Keues.Application.Features.Flows.GetFlow;
using Keues.Application.Features.Flows.UpdateFlow;

using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Features.Flows;

public static class DependencyInjection
{
  public static IServiceCollection AddFlowUseCases(this IServiceCollection services)
  {
    services.AddScoped<CreateFlowHandler>();
    services.AddScoped<UpdateFlowHandler>();
    services.AddScoped<DeleteFlowHandler>();
    services.AddScoped<GetFlowHandler>();
    services.AddScoped<GetAllFlowsHandler>();

    services.AddScoped<FlowsUseCases>();

    return services;
  }
}