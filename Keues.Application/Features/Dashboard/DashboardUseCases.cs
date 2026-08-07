using Keues.Application.Features.Dashboard.GetDashboardSummary;

namespace Keues.Application.Features.Dashboard;

public class DashboardUseCases(GetDashboardSummaryHandler getDashboardSummary)
{
  public GetDashboardSummaryHandler Summary => getDashboardSummary;
}