using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;
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
    var userGroup = await _context.UserGroups.Include(ug => ug.Users).FirstOrDefaultAsync(u=>u.Id==query.Id);

    if (userGroup == null)
    {
      throw new Exception("User group not found");
    }

    return new UserGroupBaseResult(userGroup.Id, userGroup.Name, userGroup.Color, userGroup.LocationId,userGroup.CreatedAt,
      userGroup.Users.Select(u => new UserBasic { Id = u.Id, Name = u.Name }));
  }
}