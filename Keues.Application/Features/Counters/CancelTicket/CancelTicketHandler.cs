using Keues.Application.Common;
using Keues.Domain.Entities;
using Keues.Application.Events;
using Keues.Domain.Events;

namespace Keues.Application.Features.Counters.CancelTicket;

public class CancelTicketHandler
{
  private readonly IApplicationDbContext _context;
private readonly IKeuesEventPublisher _publisher;
  public CancelTicketHandler(IApplicationDbContext context, IKeuesEventPublisher publisher)
  {
    _context = context;
    _publisher = publisher;
  }

  public async Task Handle(CancelTicketCommand request)
  {
    var ticket = await _context.Tickets.FindAsync(request.TicketId);
    if (ticket == null)
      throw new Exception($"Ticket {request.TicketId} not found");
    ticket.Cancel();
    var history = new TicketHistory
    {
      TicketId = ticket.Id,
      Event = HistoryEventTypes.Ticket.Canceled,
      CreatedAt = DateTime.UtcNow,
      CounterId = request.CounterId,
      UserId = request.UserId
    };
    await _context.TicketHistories.AddAsync(history);
    await _context.SaveChangesAsync();
    await _publisher.Publish(new TicketCanceled(ticket.Id));
  }
}