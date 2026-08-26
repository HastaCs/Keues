namespace Keues.API.Common;

public static class AppendAuthCookie
{
  public const string Name = "access_token";

  public static void Append(HttpResponse response, string jwt)
  {
    response.Cookies.Append(Name, jwt, new CookieOptions
    {
      HttpOnly = true,
      Secure = response.HttpContext.Request.IsHttps,
      SameSite = SameSiteMode.Lax,
      Expires = DateTimeOffset.UtcNow.AddYears(1)
    });
  }

  public static void Delete(HttpResponse response)
  {
    response.Cookies.Delete(Name, new CookieOptions
    {
      HttpOnly = true,
      Secure = response.HttpContext.Request.IsHttps,
      SameSite = SameSiteMode.Lax
    });
  }
}