namespace Keues.Application.Features.Users.GetUser;

public record GetUserResult(Guid Id, string Name, string Email, Guid? LocationId, DateTime CreatedAt, bool Enabled);
