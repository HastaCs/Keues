namespace Keues.Application.Features.UserGroups.UpdateUserGroup;

public record UpdateUserGroupCommand(Guid Id, string Name, string Color, Guid LocationId); 