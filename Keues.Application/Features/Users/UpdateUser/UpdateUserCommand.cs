using System.ComponentModel.DataAnnotations;

namespace Keues.Application.Features.Users.UpdateUser;

public record UpdateUserCommand(Guid Id, string Name,[EmailAddress] string Email, string Password,Guid LocationId);