using Keues.Application.Common;

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
    if(ticket == null)
      throw new Exception($"Ticket {request.TicketId} not found");
    ticket.Cancel();
    await _context.SaveChangesAsync();
  }
}