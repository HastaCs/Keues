using Keues.Application.Common;

namespace Keues.Application.Features.Locations.UpdateLocation;

public class UpdateLocationHandler
{
  private readonly IApplicationDbContext _context;

  public UpdateLocationHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<LocationBaseResponse> Handle(UpdateLocationCommand command)
  {
    var location = await _context.Locations.FindAsync(command.Id);
    if (location == null)
      throw new Exception($"Location with id '{command.Id}' not found.");

    location.Name = command.Name;
    location.Description = command.Description;
    location.Color = command.Color;

    await _context.SaveChangesAsync();
    return new LocationBaseResponse(location.Id, location.Name, location.Description,location.Color);
  }
}