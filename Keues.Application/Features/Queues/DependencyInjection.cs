
using Keues.Application.Features.Queues.CreateNewTicket;
using Keues.Application.Features.Queues.CreateQueue;
using Keues.Application.Features.Queues.DeleteQueue;
using Keues.Application.Features.Queues.GetAllQueues;
using Keues.Application.Features.Queues.GetQueue;
using Keues.Application.Features.Queues.UpdateQueue;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Features.Queues;

public static class DependencyInjection
{
  public static IServiceCollection AddTicketTypesUseCases(this IServiceCollection services)
  {
    services.AddScoped<CreateQueueHandler>();
    services.AddScoped<UpdateQueueHandler>();
    services.AddScoped<DeleteQueueHandler>();
    services.AddScoped<GetQueueHandler>();
    services.AddScoped<GetAllQueuesHandler>();
    services.AddScoped<CreateNewTicketHandler>();
    services.AddScoped<QueuesUseCases>();
  
    return services;
  }
}