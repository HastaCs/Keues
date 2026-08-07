using System.ComponentModel.DataAnnotations;

namespace Keues.Application.Features.Users.CreateAdmin;

public record CreateAdminCommand(string Name, [EmailAddress] string Email, string Password);