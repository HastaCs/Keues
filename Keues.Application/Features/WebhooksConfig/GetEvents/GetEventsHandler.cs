using Keues.Domain.Events;

namespace Keues.Application.Features.WebhooksConfig.GetEvents;

public class GetEventsHandler
{
  public dynamic Handle()
  {
    return new
    {
      TicketEvents = new List<string>
      {
        EventTypes.Ticket.Attended,
        EventTypes.Ticket.Canceled,
        EventTypes.Ticket.Called,
        EventTypes.Ticket.Created,
        EventTypes.Ticket.Transferred
      }
    };
  }
}