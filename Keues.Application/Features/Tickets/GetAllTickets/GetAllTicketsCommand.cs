using Keues.Domain.Enums;

namespace Keues.Application.Features.Tickets.GetAllTickets;

public record GetAllTicketsCommand
{
  public TicketStatus? Status { get; init; }
  public DateTime? CreatedFrom { get; init; }
  public DateTime? CreatedTo { get; init; }
  public string? Code { get; init; }
  public Guid? LocationId { get; init; }
  public Guid? QueueId { get; init; }
  public int Page { get; init; } = 1;
  public int Limit { get; init; } = 20;
}