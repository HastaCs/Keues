
using Keues.Application.Features.Queues.CreateNewTicket;
using Keues.Application.Features.Queues.CreateQueue;
using Keues.Application.Features.Queues.DeleteQueue;
using Keues.Application.Features.Queues.GetAllQueues;
using Keues.Application.Features.Queues.GetQueue;
using Keues.Application.Features.Queues.UpdateQueue;

namespace Keues.Application.Features.Queues;

public class QueuesUseCases(
  CreateQueueHandler create,
  UpdateQueueHandler update,
  DeleteQueueHandler delete,
  GetQueueHandler get,
  GetAllQueuesHandler getAll,
  CreateNewTicketHandler createTicket){

  public CreateQueueHandler Create { get; } = create;
  public UpdateQueueHandler Update { get; } = update;
  public DeleteQueueHandler Delete { get; } = delete;
  public GetQueueHandler Get { get; } = get;
  public GetAllQueuesHandler GetAll { get; } = getAll;
  public CreateNewTicketHandler CreateNewTicket { get; } = createTicket;

}
