using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Users.ResetPassword;

public class ResetPasswordHandler
{
  private readonly IApplicationDbContext _context;
  private readonly IJwtService _jwtService;

  public ResetPasswordHandler(IApplicationDbContext context, IJwtService jwtService)
  {
    _context = context;
    _jwtService = jwtService;
  }

  public async Task Handle(ResetPasswordCommand request)
  {
    var userId = _jwtService.ValidatePasswordResetToken(request.Token);
    if (userId == null)
    {
      throw new Exception("The link is invalid or has expired.");
    }

    var email = request.Email.ToLower();
    var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId && x.Email == email);
    if (user == null)
    {
      throw new Exception("The link is invalid or has expired.");
    }

    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
    await _context.SaveChangesAsync();
  }
}