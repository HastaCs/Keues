namespace Keues.Application.Features.Users.EnableDisableUser;

public record EnableDisableUserCommand(Guid Id, bool IsEnabled);