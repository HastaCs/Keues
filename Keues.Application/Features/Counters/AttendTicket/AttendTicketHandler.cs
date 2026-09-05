using Keues.Application.Common;
using Keues.Domain.Entities;
using Keues.Domain.Events;

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
    var history = new TicketHistory
    {
      Id = Guid.NewGuid(),
      TicketId = ticket.Id,
      Event = KeuesEventsType.Ticket.Attended,
      CreatedAt = DateTime.UtcNow,
      CounterId = request.CounterId
    };
   await _context.TicketHistories.AddAsync(history);
    
   var result= await _context.SaveChangesAsync();
  
  }
}