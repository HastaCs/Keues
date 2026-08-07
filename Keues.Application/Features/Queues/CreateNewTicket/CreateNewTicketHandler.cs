using Keues.Application.Common;
using Keues.Domain.Entities;

namespace Keues.Application.Features.Queues.CreateNewTicket;

public class CreateNewTicketHandler
{
  private readonly IApplicationDbContext _context;

  public CreateNewTicketHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<CreateNewTicketResponse> Handle(CreateNewTicketCommand request)
  {
    var ticketType = await _context.Queues.FindAsync(request.QueueId);
    if (ticketType == null)
    {
      throw new Exception("Ticket type not found");
    }

    var ticket = ticketType.CreateNewTicket(request.FlowId);
    _context.Tickets.Add(ticket);
    await _context.SaveChangesAsync();

    return new CreateNewTicketResponse(ticket.Id, ticket.Code);
  }
}