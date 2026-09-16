using Keues.Application.Common;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Keues.Application.Features.UserGroups.GetUserGroup;

public class GetUserGroupHandler
{
  private readonly IApplicationDbContext _context;

  public GetUserGroupHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<UserGroupBaseResult> Handle(GetUserGroupQuery query)
  {
    var userGroup = await _context.UserGroups.FindAsync(query.Id);

    if (userGroup == null)
    {
      throw new Exception("User group not found");
    }

    return new UserGroupBaseResult(userGroup.Id, userGroup.Name, userGroup.Color, userGroup.LocationId,userGroup.CreatedAt);
  }
}