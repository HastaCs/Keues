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
    var userGroup = await _context.UserGroups
      .Include(ug => ug.Users)
      .FirstOrDefaultAsync(ug => ug.Id == command.Id);

    if (userGroup == null)
    {
      throw new Exception("User group not found");
    }

    var exists = await _context.UserGroups.AnyAsync(ug =>
      ug.Name.ToLower() == command.Name.ToLower() && ug.LocationId == command.LocationId && ug.Id != command.Id);
    if (exists)
    {
      throw new Exception("User group with the same name already exists in this location");
    }

    var users = await _context.Users.Where(u => command.UserIds.Contains(u.Id) && u.LocationId == command.LocationId && u.Enabled)
      .ToListAsync();
    userGroup.Name = command.Name;
    userGroup.Color = command.Color;
    userGroup.LocationId = command.LocationId;
    userGroup.Users = users;

    await _context.SaveChangesAsync();

    return new UserGroupBaseResult(userGroup.Id, userGroup.Name, userGroup.Color, userGroup.LocationId, userGroup.CreatedAt, 
                  users.Select(u => new UserBasic { Id = u.Id, Name = u.Name }).ToList());
  }
}