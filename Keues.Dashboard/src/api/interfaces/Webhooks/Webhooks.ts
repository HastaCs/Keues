export interface WebhooksConfig {
  id: string;
  url: string;
  key: string;
  events: string[];
  enabled: boolean;
}

export interface UpdateWebhooksConfigInput {
  url: string;
  key: string;
  events: string[];
  enabled: boolean;
}

export type WebhookEventGroups = Record<string, string[]>;
