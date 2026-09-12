namespace Keues.API.Dtos.Requests.Users;

public record UpdateUserRequest(string Name, string Email, string Password,Guid LocationId);