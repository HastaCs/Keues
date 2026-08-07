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
}