using System.ComponentModel.DataAnnotations;

namespace Keues.Application.Features.Users.CreateUser;

public record CreateUserCommand(string Name, [EmailAddress] string Email, string Password,Guid LocationId);