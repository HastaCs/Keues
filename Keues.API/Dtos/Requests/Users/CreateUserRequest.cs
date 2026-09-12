namespace Keues.API.Dtos.Requests.Users;

public record CreateUserRequest(string Name, string Email, string Password,Guid LocationId);