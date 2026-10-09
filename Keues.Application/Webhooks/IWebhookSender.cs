namespace Keues.Application.Events;

public interface IWebhookSender
{
  Task SendAsync(object payload,CancellationToken cancellationToken = default);
}