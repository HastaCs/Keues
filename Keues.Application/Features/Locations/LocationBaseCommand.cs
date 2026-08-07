namespace Keues.Application.Features.Locations;

public record LocationBaseCommand
{
  public string Name { get; init; }
  public string Description { get; init; }
  public string Color { get; init; } = "blue";

}