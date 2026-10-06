using Keues.Application.Common;
using Keues.Domain.Entities;
using Keues.Application.Events;
using Keues.Domain.Events;

namespace Keues.Application.Features.Counters.AttendTicket;

public class AttendTicketHandler
{
  private readonly IApplicationDbContext _context;
  private readonly IKeuesEventPublisher _eventPublisher;

  /// <summary>
  /// Atiende un ticket en concreto
  /// </summary>
  /// <param name="context"></param>
  /// <param name="eventPublisher"></param>
  public AttendTicketHandler(IApplicationDbContext context, IKeuesEventPublisher eventPublisher)
  {
    _context = context;
    _eventPublisher = eventPublisher;
  }

  public async Task Handle(AttendTicketCommand request)
  {
    var counter = await _context.Counters.FindAsync(request.CounterId);
    if (counter == null)
    {
      throw new Exception($"Counter {request.CounterId} not found");
    }

    var ticket = await _context.Tickets.FindAsync(request.TicketId);
    if (ticket == null)
    {
      throw new Exception($"Ticket {request.TicketId} not found");
    }

    ticket.Attend();
    var history = new TicketHistory
    {
      Id = Guid.NewGuid(),
      TicketId = ticket.Id,
      Event = EventTypes.Ticket.Attended,
      CreatedAt = DateTime.UtcNow,
      CounterId = request.CounterId,
      UserId = request.UserId
    };
    await _context.TicketHistories.AddAsync(history);

    await _context.SaveChangesAsync();

    await _eventPublisher.Publish(new TicketAttended(ticket.Id, request.CounterId));
  }
}