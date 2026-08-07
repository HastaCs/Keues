namespace Keues.Application.Features.Users.CreateAdmin;

public record CreateAdminResponse(Guid Id, string Name, string Email, string Jwt);