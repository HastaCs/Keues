using System.Collections.Concurrent;
using Keues.Application.Common;

namespace Keues.Tests.Infrastructure;

/// <summary>
/// Sustituye al servicio SMTP real y registra los emails enviados en memoria,
/// permitiendo a los tests esperar/inspeccionar los mensajes.
/// </summary>
public sealed class FakeEmailService : IEmailService
{
  private readonly ConcurrentQueue<EmailMessage> _sent = new();
  private readonly SemaphoreSlim _signal = new(0);

  public record EmailMessage(string To, string Subject, string HtmlBody);

  public IReadOnlyList<EmailMessage> Sent => _sent.ToArray();

  public Task SendAsync(string to, string subject, string htmlBody)
  {
    _sent.Enqueue(new EmailMessage(to, subject, htmlBody));
    _signal.Release();
    return Task.CompletedTask;
  }

  public async Task<EmailMessage> WaitForEmailAsync(TimeSpan? timeout = null)
  {
    if (await _signal.WaitAsync(timeout ?? TimeSpan.FromSeconds(5)))
    {
      return _sent.ToArray().Last();
    }

    throw new TimeoutException("No email was sent.");
  }
}
