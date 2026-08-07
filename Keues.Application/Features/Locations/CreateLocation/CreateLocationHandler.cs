using Keues.Application.Common;
using Keues.Domain.Entities;

namespace Keues.Application.Features.Locations.CreateLocation;

public class CreateLocationHandler
{
  private readonly IApplicationDbContext _context;
  
  public CreateLocationHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  
  public async Task<LocationBaseResponse> Handle(CreateLocationCommand command)
  {
    var location = Location.Create(command.Name, command.Description,command.Color);
    await _context.Locations.AddAsync(location);
    await _context.SaveChangesAsync();
    return new LocationBaseResponse(location.Id, location.Name, location.Description, location.Color);
  }
}