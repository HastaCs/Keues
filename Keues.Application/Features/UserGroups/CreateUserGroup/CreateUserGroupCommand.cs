namespace Keues.Application.Features.UserGroups.CreateUserGroup;

public record CreateUserGroupCommand(string Name, string Color, Guid LocationId);