using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Keues.Application.Common;

using Keues.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Keues.Infrastructure.Authorization;

public class JwtService : IJwtService
{
  private const string PasswordResetPurpose = "password_reset";
  private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(15);

  private readonly JwtOptions _options;

  public JwtService(IOptions<JwtOptions> options)
  {
    _options = options.Value;
  }

  public string Generate(User user)
  {
    var claims = new List<Claim>
    {
      new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
      new(JwtRegisteredClaimNames.Email, user.Email),
      new(ClaimTypes.Role, user.Role.ToString())
    };

    return WriteToken(claims, DateTime.UtcNow.AddMinutes(_options.ExpirationInMinutes));
  }

  public string GeneratePasswordResetToken(User user)
  {
    var claims = new List<Claim>
    {
      new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
      new(JwtRegisteredClaimNames.Email, user.Email),
      new("purpose", PasswordResetPurpose)
    };

    return WriteToken(claims, DateTime.UtcNow.Add(ResetTokenLifetime));
  }

  public Guid? ValidatePasswordResetToken(string token)
  {
    try
    {
      var handler = new JwtSecurityTokenHandler();
      var principal = handler.ValidateToken(token, CreateValidationParameters(), out var _);

      var purpose = principal.FindFirst("purpose")?.Value;
      if (purpose != PasswordResetPurpose)
      {
        return null;
      }

      var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      return Guid.TryParse(sub, out var userId) ? userId : null;
    }
    catch (Exception)
    {
      return null;
    }
  }

  private string WriteToken(IEnumerable<Claim> claims, DateTime expires)
  {
    var key = new SymmetricSecurityKey(
      Encoding.UTF8.GetBytes(_options.Key));

    var credentials = new SigningCredentials(
      key,
      SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
      issuer: _options.Issuer,
      audience: _options.Audience,
      claims: claims,
      expires: expires,
      signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  private TokenValidationParameters CreateValidationParameters()
  {
    return new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateLifetime = true,
      ValidateIssuerSigningKey = true,

      ValidIssuer = _options.Issuer,
      ValidAudience = _options.Audience,

      IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(_options.Key)),

      ClockSkew = TimeSpan.FromSeconds(30)
    };
  }
}