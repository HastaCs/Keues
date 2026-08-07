namespace Keues.Application.Features.Dashboard.GetDashboardSummary;

public record GetDashboardSummaryCommand
{
  public Guid LocationId { get; init; }
  public DateTime? Date { get; init; }
}
