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
        if(exists)
        {
            throw new Exception("User group with the same name already exists in this location");
        }
        var userGroup = new UserGroup
        {
            Name = command.Name,
            Color = command.Color,
            LocationId = command.LocationId,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserGroups.Add(userGroup);
        await _context.SaveChangesAsync();

        return new UserGroupBaseResult(userGroup.Id, userGroup.Name, userGroup.Color, userGroup.LocationId,userGroup.CreatedAt);
    }
}