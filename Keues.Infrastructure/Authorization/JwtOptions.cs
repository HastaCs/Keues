namespace Keues.Infrastructure.Authorization;

public class JwtOptions
{
  public const string SectionName = "Jwt";

  public string Key { get; set; } = string.Empty;

  public string Issuer { get; set; } = string.Empty;

  public string Audience { get; set; } = string.Empty;

  public int ExpirationInMinutes { get; set; } = 60*24*10000;
}