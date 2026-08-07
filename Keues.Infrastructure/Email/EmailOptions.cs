namespace Keues.Infrastructure.Email;

public class EmailOptions
{
  public string Provider { get; set; } = "smtp";

  public SmtpOptions Smtp { get; set; } = new();
}