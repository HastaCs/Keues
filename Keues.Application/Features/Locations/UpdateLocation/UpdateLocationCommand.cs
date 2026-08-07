namespace Keues.Application.Features.Locations.UpdateLocation;

public record UpdateLocationCommand:LocationBaseCommand
{
  public Guid Id { get; set; }
}