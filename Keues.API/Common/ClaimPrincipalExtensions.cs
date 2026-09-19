using System.Security.Claims;

namespace Keues.API.Common;

public static class ClaimsPrincipalExtensions
{
  public static Guid? GetUserId(this ClaimsPrincipal user)
  {
    // "sub" se mapea a ClaimTypes.NameIdentifier por defecto (herencia WIF de .NET)
    var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return Guid.TryParse(value, out var id) ? id : null;
  }
}