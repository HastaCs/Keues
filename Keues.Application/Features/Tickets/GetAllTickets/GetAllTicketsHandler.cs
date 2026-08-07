using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Tickets.GetAllTickets;

public class GetAllTicketsHandler
{
  private readonly IApplicationDbContext _context;
  public GetAllTicketsHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  
  public async Task<IEnumerable<GetTicketResponse>> Handle(GetAllTicketsCommand request)
  {
    var ticketQuery = _context.Tickets.AsQueryable();
    if(request.Code != null)
    {
      ticketQuery = ticketQuery.Where(t => t.Code == request.Code);
    }
    if(request.CreatedFrom != null)
    {
      ticketQuery = ticketQuery.Where(t => t.CreatedAt >= request.CreatedFrom);
    }
    if(request.CreatedTo != null)
    {
      ticketQuery = ticketQuery.Where(t => t.CreatedAt <= request.CreatedTo);
    }
    if(request.LocationId != null)
    {
      ticketQuery = ticketQuery.Where(t => t.Queue.LocationId == request.LocationId);
    }
    if(request.QueueId != null)
    {
      ticketQuery = ticketQuery.Where(t => t.QueueId == request.QueueId);
    }
   
    if (request.Status != null)
    {
      ticketQuery = ticketQuery.Where(t => t.Status == request.Status);
    }
    

    var tickets = await ticketQuery.Include(t => t.Queue)
                                              .Include(t => t.Counter)
                                              .Include(t => t.Flow).ToListAsync();
    return tickets.Select(ticket => new GetTicketResponse
    {
      Id = ticket.Id,
      Status = ticket.Status,
      CreatedAt = ticket.CreatedAt,
      CalledAt = ticket.CalledAt,
      AttendedAt = ticket.AttendedAt,
      CanceledAt = ticket.CanceledAt,
      Queue = new QueueMin { Id = ticket.Queue.Id, Name = ticket.Queue.Name },
      Counter = ticket.Counter != null ? new CounterMin { Id = ticket.Counter.Id, Name = ticket.Counter.Name } : null,
      Flow = new FlowMin { Id = ticket.FlowId, Name = ticket.Flow.Name },
      Code = ticket.Code
    });
  }
}