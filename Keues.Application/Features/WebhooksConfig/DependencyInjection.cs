using Keues.Application.Features.WebhooksConfig.GetEvents;
using Keues.Application.Features.WebhooksConfig.GetWebhooksConfig;
using Keues.Application.Features.WebhooksConfig.IsSubscribed;
using Keues.Application.Features.WebhooksConfig.UpdateWebhooksConfig;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Features.WebhooksConfig;

public static class DependencyInjection
{
    public static IServiceCollection AddWebhooksConfigUseCases(this IServiceCollection services)
    {
        services.AddScoped<GetWebhooksConfigHandler>();
        services.AddScoped<UpdateWebhooksConfigHandler>();
        services.AddScoped<GetEventsHandler>();
        services.AddScoped<IsSubscribedHandler>();
        
        services.AddScoped<WebhooksConfigUseCases>();
    
        return services;
    }
}