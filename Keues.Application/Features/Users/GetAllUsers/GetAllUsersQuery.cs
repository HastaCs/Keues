using Keues.Domain.Enums;

namespace Keues.Application.Features.Users.GetAllUsers;

public record GetAllUsersQuery
{
  public Guid? LocationId { get; init; }
  
  public string? Name { get; init; }
  
  public bool? IsActive { get; init; }
  
  public int Page { get; init; } = 1;
  public int Limit { get; init; } = 20;
  public SortOrder SortOrder { get; init; } = SortOrder.Desc;
}