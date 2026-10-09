using Keues.Application.Events.Handlers.Tickets;
using Keues.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Events.Handlers;

public static class DependencyInjection
{
  public static IServiceCollection AddKeuesEventHandlers(this IServiceCollection services)
  {
    // Register all event handlers in the assembly
    services.AddScoped<IKeuesEventHandler<TicketCreated>, TicketCreatedHandler>();
    services.AddScoped<IKeuesEventHandler<TicketTransferred>, TicketTransferredHandler>();
    services.AddScoped<IKeuesEventHandler<TicketCanceled>, TicketCanceledHandler>();
    services.AddScoped<IKeuesEventHandler<TicketCalled>, TicketCalledHandler>();
    services.AddScoped<IKeuesEventHandler<TicketAttended>, TicketAttendedHandler>();
    
    return services;
  }
}