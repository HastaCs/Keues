using Keues.Application.Common;
using Keues.Application.Features.UserGroups.GetUserGroup;

using Keues.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.UserGroups.GetAllUserGroups;

public class GetAllUserGroupsHandler
{
  private readonly IApplicationDbContext _context;
  
  public GetAllUserGroupsHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetAllUserGroupsResult> Handle(GetAllUserGroupsQuery request)
    {
        var userGroups = _context.UserGroups.AsQueryable();

        if (request.LocationId.HasValue)
            userGroups = userGroups.Include(u=>u.Users).Where(ug => ug.LocationId == request.LocationId.Value);
        
        
        var result=userGroups.Select(ug => new UserGroupBaseResult(ug.Id,ug.Name, ug.Color,ug.LocationId,ug.CreatedAt,
            ug.Users.Select(u => new UserBasic { Id = u.Id, Name = u.Name }))).ToList();
        
        return new GetAllUserGroupsResult(result);
        
        
       
    }
}