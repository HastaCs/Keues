
using Keues.Application.Features.Counters.AttendTicket;
using Keues.Application.Features.Counters.CallNextTicket;
using Keues.Application.Features.Counters.CancelTicket;
using Keues.Application.Features.Counters.CreateCounter;
using Keues.Application.Features.Counters.DeleteCounter;
using Keues.Application.Features.Counters.GetAllCounters;
using Keues.Application.Features.Counters.GetCounter;
using Keues.Application.Features.Counters.GetQueues;
using Keues.Application.Features.Counters.UpdateCounter;

namespace Keues.Application.Features.Counters;

public class CounterUseCases(CreateCounterHandler create,UpdateCounterHandler update, DeleteCounterHandler delete, GetCounterHandler get, GetAllCountersHandler getAll,
  CallNextTicketHandler callNextTicket, AttendTicketHandler attendTicket,CancelTicketHandler cancelTicket,GetQueuesHandle getQueuesHandle)
{
  public CreateCounterHandler Create { get; } = create;
  public UpdateCounterHandler Update { get; } = update;
  public DeleteCounterHandler Delete { get; } = delete;
  public GetCounterHandler Get { get; } = get;
  public GetAllCountersHandler GetAll { get; } = getAll;
  public  CallNextTicketHandler CallNextTicket { get; } = callNextTicket;
  public AttendTicketHandler AttendTicket { get; } = attendTicket;
  
  public  CancelTicketHandler CancelTicket { get; } = cancelTicket;
  public  GetQueuesHandle GetQueues { get; } = getQueuesHandle;
}