using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Users.Login;

public class LoginHandler
{
  private readonly IApplicationDbContext _context;
  private readonly IJwtService _jwtService;
  public LoginHandler(IApplicationDbContext context, IJwtService jwtService)
  {
    _context = context;
    _jwtService = jwtService;
  }

  public async Task<LoginResponse> Handle(LoginCommand request)
  {
    var mail = request.Email.ToLower();
    var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == mail);
    if (user == null)
    {
      throw new Exception($"Invalid credentials");
    }

    var passWordHashed = user.PasswordHash;
  
    if (!BCrypt.Net.BCrypt.Verify(request.Password, passWordHashed))
    {
      throw new Exception($"Invalid credentials");
    }
    var token = _jwtService.Generate(user);
    return new LoginResponse( token);
  }
}