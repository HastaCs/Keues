using Keues.Application.Common;
using Keues.Domain.Entities;
using Keues.Domain.Events;

namespace Keues.Application.Features.Queues.CreateNewTicket;

public class CreateNewTicketHandler
{
  private readonly IApplicationDbContext _context;
  private readonly IKeuesEventPublisher _eventPublisher;

  public CreateNewTicketHandler(IApplicationDbContext context, IKeuesEventPublisher eventPublisher)
  {
    _context = context;
    _eventPublisher = eventPublisher;
  }

  public async Task<CreateNewTicketResponse> Handle(CreateNewTicketCommand request)
  {
    var queue = await _context.Queues.FindAsync(request.QueueId);
    if (queue == null)
    {
      throw new Exception("Ticket type not found");
    }

    var ticket = queue.CreateNewTicket(request.FlowId);
    _context.Tickets.Add(ticket);

    var history = new TicketHistory
    {
      TicketId = ticket.Id,
      Event = HistoryEventTypes.Ticket.Created,
      Counter = null,
      QueueId = queue.Id,
    };
    await _context.TicketHistories.AddAsync(history);
    await _context.SaveChangesAsync();

    await _eventPublisher.Publish(new TicketCreated(ticket.Id), CancellationToken.None);

    return new CreateNewTicketResponse(ticket.Id, ticket.Code);
  }
}