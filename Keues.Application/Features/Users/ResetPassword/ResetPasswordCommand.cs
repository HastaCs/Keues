namespace Keues.Application.Features.Users.ResetPassword;

public record ResetPasswordCommand(string Token, string Email, string Password);