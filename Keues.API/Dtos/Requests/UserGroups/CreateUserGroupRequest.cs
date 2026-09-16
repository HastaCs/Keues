namespace Keues.API.Dtos.Requests.UserGroups;

public record CreateUserGroupRequest(string Name, string Color, Guid LocationId);