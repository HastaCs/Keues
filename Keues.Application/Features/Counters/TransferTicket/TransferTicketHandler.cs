using Keues.Application.Common;
using Keues.Domain.Entities;
using Keues.Domain.Enums;
using Keues.Application.Events;
using Keues.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Counters.TransferTicket;

public class TransferTicketHandler
{
  private readonly IApplicationDbContext _context;
  private readonly IKeuesEventPublisher _eventPublisher;

  public TransferTicketHandler(IApplicationDbContext context, IKeuesEventPublisher eventPublisher)
  {
    _context = context;
    _eventPublisher = eventPublisher;
  }

  public async Task Handle(TransferTicketCommand command)
  {
    var counter = await _context.Counters.FindAsync(command.CounterId);
    if (counter == null)
    {
      throw new Exception($"Counter with Id {command.CounterId} not found.");
    }

    var ticket = await _context.Tickets.Include(q => q.Queue).FirstOrDefaultAsync(q => q.Id == command.TicketId);
    if (ticket == null)
    {
      throw new Exception($"Ticket with Id {command.TicketId} not found.");
    }

    var destinationQueue = await _context.Queues.FindAsync(command.QueueId);
    if (destinationQueue == null)
    {
      throw new Exception($"Queue with Id {command.QueueId} not found.");
    }

    //Only can change to queue in the same location
    if (destinationQueue.LocationId != ticket.Queue.LocationId)
    {
      throw new Exception($"Queue {destinationQueue.Name} is not in the same location as the ticket's queue.");
    }

    ticket.Queue = destinationQueue;
    ticket.Waiting();
    var history = new TicketHistory
    {
      TicketId = ticket.Id,
      CounterId = counter.Id,
      CreatedAt = DateTime.UtcNow,
      Event = EventTypes.Ticket.Transferred,
      QueueId = destinationQueue.Id,
      UserId = command.UserId
    };
    await _context.TicketHistories.AddAsync(history);
    await _context.SaveChangesAsync();
    await _eventPublisher.Publish(new TicketTransferred(ticket.Id, counter.Id, destinationQueue.Id));
  }
}