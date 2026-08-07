using Keues.Application.Features.Locations.CreateLocation;
using Keues.Application.Features.Locations.DeleteLocation;
using Keues.Application.Features.Locations.GetAllLocations;
using Keues.Application.Features.Locations.GetLocation;
using Keues.Application.Features.Locations.UpdateLocation;

namespace Keues.Application.Features.Locations;

public class LocationUseCases(CreateLocationHandler create,UpdateLocationHandler update, DeleteLocationHandler delete, GetAllLocationsHandler getAll, GetLocationHandler get)
{
  public CreateLocationHandler Create { get; } = create;
  public UpdateLocationHandler Update { get; } = update;
  public DeleteLocationHandler Delete { get; } = delete;
  public GetAllLocationsHandler GetAll { get; } = getAll;
  public GetLocationHandler Get { get; } = get;
  }
