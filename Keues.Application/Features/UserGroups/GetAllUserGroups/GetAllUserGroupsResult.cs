using Keues.Application.Features.UserGroups.GetUserGroup;

namespace Keues.Application.Features.UserGroups.GetAllUserGroups;

public record GetAllUserGroupsResult(IEnumerable<UserGroupBaseResult> UserGroups);
