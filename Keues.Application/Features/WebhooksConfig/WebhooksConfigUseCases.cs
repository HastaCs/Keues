using Keues.Application.Features.WebhooksConfig.GetEvents;
using Keues.Application.Features.WebhooksConfig.GetWebhooksConfig;
using Keues.Application.Features.WebhooksConfig.UpdateWebhooksConfig;

namespace Keues.Application.Features.WebhooksConfig;

public class WebhooksConfigUseCases(
  UpdateWebhooksConfigHandler updateWebhooksConfigHandler,
  GetWebhooksConfigHandler getWebhooksConfigHandler,GetEventsHandler getEventsHandler)
{
  public UpdateWebhooksConfigHandler UpdateWebhooksConfigHandler => updateWebhooksConfigHandler;
  public GetWebhooksConfigHandler GetWebhooksConfigHandler => getWebhooksConfigHandler;
  public GetEventsHandler GetEventsHandler => getEventsHandler;
}