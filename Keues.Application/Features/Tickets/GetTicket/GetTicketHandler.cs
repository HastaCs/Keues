using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Tickets.GetTicket;

public class GetTicketHandler
{
  private readonly IApplicationDbContext _context;
  public GetTicketHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<GetTicketResponse> Handle(GetTicketCommand request)
  {
    var ticket = await _context.Tickets
      .Include(x => x.Queue)
      .Include(x => x.Counter)
      .Include(x => x.Flow)
      .FirstOrDefaultAsync(x => x.Id == request.Id);

    if (ticket == null)
      throw new Exception($"Ticket with id {request.Id} not found");
    return new GetTicketResponse
    {
      Id = ticket.Id,
      Status = ticket.Status,
      CreatedAt = ticket.CreatedAt,
      CalledAt = ticket.CalledAt,
      AttendedAt = ticket.AttendedAt,
      CanceledAt = ticket.CanceledAt,
      Queue = new QueueMin { Id = ticket.Queue.Id, Name = ticket.Queue.Name },
      Counter = ticket.Counter != null ? new CounterMin { Id = ticket.Counter.Id, Name = ticket.Counter.Name } : null,
      Code = ticket.Code,
      Flow= new FlowMin { Id = ticket.FlowId, Name = ticket.Flow.Name },
      FlowId = ticket.FlowId,
      LocationId = ticket.Flow.LocationId
    };
    
  }
}