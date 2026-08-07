using Keues.Application.Features.Counters.AttendTicket;
using Keues.Application.Features.Counters.CallNextTicket;
using Keues.Application.Features.Counters.CancelTicket;
using Keues.Application.Features.Counters.CreateCounter;
using Keues.Application.Features.Counters.DeleteCounter;
using Keues.Application.Features.Counters.GetAllCounters;
using Keues.Application.Features.Counters.GetCounter;
using Keues.Application.Features.Counters.UpdateCounter;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Features.Counters;

public static class DependencyInjection
{

  public static IServiceCollection AddCountersUseCases(this IServiceCollection services)
  {
    services.AddScoped<CreateCounterHandler>();
    services.AddScoped<UpdateCounterHandler>();
    services.AddScoped<DeleteCounterHandler>();
    services.AddScoped<GetCounterHandler>();
    services.AddScoped<GetAllCountersHandler>();
    services.AddScoped<CallNextTicketHandler>();
    services.AddScoped<AttendTicketHandler>();
    services.AddScoped<CancelTicketHandler>();
    services.AddScoped<CounterUseCases>();
    
    return services;
  }
}