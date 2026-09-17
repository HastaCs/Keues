using Keues.Application.Common;
using Keues.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.UserGroups.CreateUserGroup;

public class CreateUserGroupHandler
{
  private readonly IApplicationDbContext _context;
  
  public CreateUserGroupHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserGroupBaseResult> Handle(CreateUserGroupCommand command)
    {
        var exists= await _context.UserGroups.AnyAsync(ug => ug.Name.ToLower() == command.Name.ToLower() && ug.LocationId == command.LocationId);
        //Solo usuarios de esa location
        var users= await _context.Users.Where(u => command.UserIds.Contains(u.Id) && u.LocationId==command.LocationId && u.Enabled).ToListAsync();
        if(exists)
        {
            throw new Exception("User group with the same name already exists in this location");
        }
        var userGroup = new UserGroup
        {
            Name = command.Name,
            Color = command.Color,
            LocationId = command.LocationId,
            CreatedAt = DateTime.UtcNow,
            Users = users
        };

        _context.UserGroups.Add(userGroup);
        await _context.SaveChangesAsync();

        return new UserGroupBaseResult(userGroup.Id, userGroup.Name, userGroup.Color, userGroup.LocationId,userGroup.CreatedAt,
            users.Select(u => new UserBasic { Id = u.Id, Name = u.Name })); 
    }
}