using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.UserGroups.UpdateUserGroup;

public class UpdateUserGroupHandler
{
  private readonly IApplicationDbContext _context;

  public UpdateUserGroupHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<UserGroupBaseResult> Handle(UpdateUserGroupCommand command)
  {
    var userGroup = await _context.UserGroups.FindAsync(command.Id);

    if (userGroup == null)
    {
      throw new Exception("User group not found");
    }

    var exists = await _context.UserGroups.AnyAsync(ug => ug.Name.ToLower() == command.Name.ToLower() && ug.LocationId == command.LocationId && ug.Id != command.Id);
    if (exists)
    {
      throw new Exception("User group with the same name already exists in this location");
    }

    userGroup.Name = command.Name;
    userGroup.Color = command.Color;
    userGroup.LocationId = command.LocationId;

    await _context.SaveChangesAsync();

    return new UserGroupBaseResult(userGroup.Id, userGroup.Name, userGroup.Color, userGroup.LocationId,
      userGroup.CreatedAt);
  }
}