using Keues.Application.Features.Tickets.GetAllTickets;
using Keues.Application.Features.Tickets.GetTicket;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Features.Tickets;

public static class DependencyInjection
{
  public static IServiceCollection AddTicketsUseCases(this IServiceCollection services)
  {
    services.AddScoped<GetAllTicketsHandler>();
    services.AddScoped<GetTicketHandler>();
    services.AddScoped<TicketsUseCases>();
    return services;
  }
}