using Keues.Application.Features.Locations.CreateLocation;
using Keues.Application.Features.Locations.DeleteLocation;
using Keues.Application.Features.Locations.GetAllLocations;
using Keues.Application.Features.Locations.GetLocation;
using Keues.Application.Features.Locations.UpdateLocation;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Features.Locations;

public static class DependencyInjection
{
    public static IServiceCollection AddLocationUseCases(this IServiceCollection services)
    {
        services.AddScoped<CreateLocationHandler>();
        services.AddScoped<UpdateLocationHandler>();
        services.AddScoped<DeleteLocationHandler>();
        services.AddScoped<GetAllLocationsHandler>();
        services.AddScoped<GetLocationHandler>();
    
        services.AddScoped<LocationUseCases>();
    
        return services;
    }
}