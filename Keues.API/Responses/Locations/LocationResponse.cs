using Keues.Application.Features.Locations;
using Keues.Application.Features.Locations.CreateLocation;
using Keues.Application.Features.Locations.UpdateLocation;

namespace Keues.API.Responses.Locations;

public class LocationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Color { get; set; }

    public static LocationResponse FromLocation(LocationBaseResponse location)
    {
        return new LocationResponse()
        {
            Id = location.Id,
            Name = location.Name,
            Description = location.Description,
            Color = location.Color
        };
    }
   
}