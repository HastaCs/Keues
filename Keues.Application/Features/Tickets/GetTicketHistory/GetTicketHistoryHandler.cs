using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Tickets.GetTicketHistory;

public class GetTicketHistoryHandler
{
  private readonly IApplicationDbContext _context;

  public GetTicketHistoryHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<IEnumerable<GetTicketHistoryResponse>> Handle(GetTicketRequest request)
  {
    var history = await _context.TicketHistories.Include(y=>y.User).Include(x => x.Counter).Include(x => x.Queue)
      .Where(x => x.TicketId == request.TicketId)
      .OrderBy(x => x.CreatedAt)
      .Select(x => new GetTicketHistoryResponse()
      {
        Id = x.Id,
        Event = x.Event,
        CreatedAt = x.CreatedAt,
        CounterName = x.Counter.Name,
        QueueName = x.Queue.Name,
     User = x.User != null ? new UserMin(x.User.Id, x.User.Name) : null
      })
      .ToListAsync();

    return history;
  }
}