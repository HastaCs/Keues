using Keues.Application.Features.Dashboard.GetDashboardSummary;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Features.Dashboard;

public static class DependencyInjection
{
  public static IServiceCollection AddDashboardUseCases(this IServiceCollection services)
  {
    services.AddScoped<GetDashboardSummaryHandler>();
    services.AddScoped<DashboardUseCases>();
    return services;
  }
}