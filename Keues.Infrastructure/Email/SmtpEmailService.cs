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
    var smtp = _options.Value.Smtp;

    var message = new MimeMessage();
    message.From.Add(MailboxAddress.Parse(smtp.From));
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