namespace Keues.Application.Features.WebhooksConfig.UpdateWebhooksConfig;

public record UpdateWebhooksCommand(string Url, List<string> Events, bool Enabled, string Key);
