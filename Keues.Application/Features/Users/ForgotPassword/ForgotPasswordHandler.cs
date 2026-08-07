using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Keues.Application.Features.Users.ForgotPassword;

public class ForgotPasswordHandler
{
  private readonly IApplicationDbContext _context;
  private readonly IJwtService _jwtService;
  private readonly IEmailService _emailService;
  private readonly PasswordResetOptions _options;

  public ForgotPasswordHandler(
    IApplicationDbContext context,
    IJwtService jwtService,
    IEmailService emailService,
    IOptions<PasswordResetOptions> options)
  {
    _context = context;
    _jwtService = jwtService;
    _emailService = emailService;
    _options = options.Value;
  }

  public async Task Handle(ForgotPasswordCommand request)
  {
    var email = request.Email.ToLower();
    var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

    // Never reveal whether the email exists in the system.
    if (user == null)
    {
      return;
    }

    if (string.IsNullOrWhiteSpace(_options.FrontendUrl))
    {
      throw new Exception("DashboardUrl is not configured in config.json");
    }

    var baseUrl = _options.FrontendUrl.TrimEnd('/');

    var token = _jwtService.GeneratePasswordResetToken(user);
    var link =
      $"{baseUrl}/reset-password" +
      $"?token={Uri.EscapeDataString(token)}" +
      $"&email={Uri.EscapeDataString(user.Email)}";

    var html = $"""
      <p>Hi {user.Name},</p>
      <p>You requested to reset the password of your <strong>Keues</strong> account.</p>
      <p>Click the link below to choose a new password. It expires in 15 minutes:</p>
      <p><a href="{link}">Reset password</a></p>
      <p>If you did not request this change, you can ignore this email.</p>
      """;

    await _emailService.SendAsync(
      user.Email,
      "Keues - Reset password",
      html);
  }
}