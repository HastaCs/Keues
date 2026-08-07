using Keues.Domain.Enums;

namespace Keues.Application.Features.Users.Me;

public record MeResponse(Guid Id, string Name, string Email, Rol Role);
