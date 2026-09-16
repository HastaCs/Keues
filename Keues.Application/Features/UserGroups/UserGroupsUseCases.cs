using Keues.Application.Features.UserGroups.CreateUserGroup;
using Keues.Application.Features.UserGroups.DeleteUserGroup;
using Keues.Application.Features.UserGroups.GetAllUserGroups;
using Keues.Application.Features.UserGroups.GetUserGroup;
using Keues.Application.Features.UserGroups.UpdateUserGroup;

namespace Keues.Application.Features.UserGroups;

public class UserGroupsUseCases(
  GetAllUserGroupsHandler getAllUserGroups,
  GetUserGroupHandler getUserGroup,
  CreateUserGroupHandler createUserGroup,
  UpdateUserGroupHandler updateUserGroup,
  DeleteUserGroupHandler deleteUserGroup)
{
  public GetAllUserGroupsHandler GetAllUserGroups => getAllUserGroups;
  public GetUserGroupHandler GetUserGroup => getUserGroup;
  public CreateUserGroupHandler CreateUserGroup => createUserGroup;
  public UpdateUserGroupHandler UpdateUserGroup => updateUserGroup;
  public DeleteUserGroupHandler DeleteUserGroup => deleteUserGroup;
}