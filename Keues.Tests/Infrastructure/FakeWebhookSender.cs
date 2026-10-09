using Keues.Application.Events;

namespace Keues.Tests.Infrastructure;

public class FakeWebhookSender : IWebhookSender
{
  public List<object> Sent { get; } = [];

  public Task SendAsync(object payload, CancellationToken cancellationToken = default)
  {
    Sent.Add(payload);
    return Task.CompletedTask;
  }
}
