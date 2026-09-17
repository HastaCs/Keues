using Keues.API.Dtos.Requests.UserGroups;
using Keues.Application.Features.UserGroups.UpdateUserGroup;
using Keues.Application.Features.UserGroups.CreateUserGroup;

namespace Keues.API.Mappers;

public static class UserGroupMapper
{
  public static UpdateUserGroupCommand ToCommand(this UpdateUserGroupRequest request, Guid id)
  {
    return new UpdateUserGroupCommand(id, request.Name, request.Color, request.LocationId, request.UserIds);
  }
  public static CreateUserGroupCommand ToCommand(this CreateUserGroupRequest request)
  {
    return new CreateUserGroupCommand(request.Name, request.Color, request.LocationId, request.UserIds);
  }
  
}