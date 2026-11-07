namespace Keues.Infrastructure.Email;

public class EmailOptions
{
  public string Provider { get; set; } = "smtp";

  /// <summary>
  /// Sender address shared by every provider.
  /// </summary>
  public string From { get; set; } = string.Empty;

  /// <summary>
  /// Sender display name shared by every provider.
  /// </summary>
  public string FromName { get; set; } = "Keues";

  public SmtpOptions Smtp { get; set; } = new();

  public MailjetOptions Mailjet { get; set; } = new();
}