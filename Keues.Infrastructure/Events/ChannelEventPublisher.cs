using System.Diagnostics;
using System.Threading.Channels;
using Keues.Domain.Events;

namespace Keues.Infrastructure.Events;

public class ChannelEventPublisher:IKeuesEventPublisher
{
  private readonly Channel<IKeuesEvent> _channel;
  
  public ChannelEventPublisher(Channel<IKeuesEvent> channel)
  {
    _channel = channel;
  }
  
  public async Task Publish<TEvent>(TEvent keuesEvent, CancellationToken cancellationToken = default) where TEvent : IKeuesEvent
  {
    Debug.WriteLine("🔥 PUBLICANDO: {EventName}", keuesEvent.GetType().Name);
    await _channel.Writer.WriteAsync(keuesEvent, cancellationToken);
  }
}