using Keues.Application.Common;

namespace Keues.Application.Features.Counters.AttendTicket;

public class AttendTicketHandler
{
  private readonly IApplicationDbContext _context;

  public AttendTicketHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task Handle(AttendTicketCommand request)
  {
    var counter=await _context.Counters.FindAsync(request.CounterId);
    if (counter == null)
    {
      throw new Exception($"Counter {request.CounterId} not found");
    }
    var ticket=await _context.Tickets.FindAsync(request.TicketId);
    if (ticket == null)
    {
      throw new Exception($"Ticket {request.TicketId} not found");
    }

    ticket.Attend();
    await _context.SaveChangesAsync();

  }
}