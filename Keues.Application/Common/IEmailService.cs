namespace Keues.Application.Common;

public interface IEmailService
{
  Task SendAsync(string to, string subject, string htmlBody);
}