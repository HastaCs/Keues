namespace Keues.Infrastructure.Email;

public class SmtpOptions
{
  public string Host { get; set; } = string.Empty;

  public int Port { get; set; } = 587;

  public string User { get; set; } = string.Empty;

  public string Password { get; set; } = string.Empty;

  public string From { get; set; } = string.Empty;

  public bool UseTls { get; set; } = true;
}