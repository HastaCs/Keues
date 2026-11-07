using Keues.Application.Common;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Keues.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
  private readonly IOptions<EmailOptions> _options;

  public SmtpEmailService(IOptions<EmailOptions> options)
  {
    _options = options;
  }

  public async Task SendAsync(string to, string subject, string htmlBody)
  {
    var options = _options.Value;
    var smtp = options.Smtp;

    // Email:Smtp:From is kept as an optional override for backwards
    // compatibility; the shared Email:From is the default sender.
    var from = !string.IsNullOrWhiteSpace(smtp.From) ? smtp.From : options.From;
    if (string.IsNullOrWhiteSpace(from))
      throw new InvalidOperationException(
        "Email From is not configured (KEUES_EMAIL_FROM).");

    var fromName = string.IsNullOrWhiteSpace(options.FromName) ? "Keues" : options.FromName;

    var message = new MimeMessage();
    message.From.Add(new MailboxAddress(fromName, from));
    message.To.Add(MailboxAddress.Parse(to));
    message.Subject = subject;
    message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

    using var client = new SmtpClient();
    await client.ConnectAsync(
      smtp.Host,
      smtp.Port,
      smtp.UseTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

    if (!string.IsNullOrEmpty(smtp.User))
    {
      await client.AuthenticateAsync(smtp.User, smtp.Password);
    }

    await client.SendAsync(message);
    await client.DisconnectAsync(true);
  }
}