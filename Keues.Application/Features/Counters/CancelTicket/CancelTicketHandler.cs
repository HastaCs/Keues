using Keues.Application.Common;
using Keues.Domain.Entities;
using Keues.Domain.Events;

namespace Keues.Application.Features.Counters.CancelTicket;

public class CancelTicketHandler
{
  private readonly IApplicationDbContext _context;

  public CancelTicketHandler(IApplicationDbContext context)
  {
    _context = context;
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
      Event = KeuesEventsType.Ticket.Canceled,
      CreatedAt = DateTime.UtcNow,
      CounterId = request.CounterId
    };
    await _context.TicketHistories.AddAsync(history);
    await _context.SaveChangesAsync();
  }
}