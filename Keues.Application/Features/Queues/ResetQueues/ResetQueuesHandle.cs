using Keues.Application.Common;
using Keues.Domain.Enums;
using Keues.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Queues.ResetQueues;

public class ResetQueuesHandle
{
  private readonly IApplicationDbContext _context;

  public ResetQueuesHandle(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task Handle()
  {
    DateTime now = DateTime.UtcNow;
    TimeOnly timeNow = new TimeOnly(now.Hour, now.Minute); 
    var queuesToReset = await _context.Queues.Where(q => q.ResetAt == timeNow).ToListAsync();
    foreach (var queue in queuesToReset)
    {
      if(queue.LastResetAt.HasValue && queue.LastResetAt.Value.Date == now.Date)
      {
        continue; //Ya se reseteo hoy
      }
      //Tickets de esa queue
      queue.LastResetAt = now;
      queue.NextNumber = 1;
      var ticketsToReset = await _context.Tickets.Where(t => t.QueueId == queue.Id && (t.Status==TicketStatus.InProgress || t.Status==TicketStatus.Waiting)).ToListAsync();
      foreach (var ticket in ticketsToReset)
      {
        
        ticket.Cancel();
        var ticketHistory = new Domain.Entities.TicketHistory
        {
          Id = Guid.NewGuid(),
          TicketId = ticket.Id,
          Event = KeuesEventsType.Ticket.Canceled,
          CreatedAt = now,
          Ticket =  ticket,  
        };
        await _context.TicketHistories.AddAsync(ticketHistory);
      }
      await _context.SaveChangesAsync();
    }
    
  }
}