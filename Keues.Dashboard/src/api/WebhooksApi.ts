import { request } from './httpClient';
import type {
  UpdateWebhooksConfigInput,
  WebhookEventGroups,
  WebhooksConfig,
} from './interfaces/Webhooks/Webhooks';

const endpoint = '/WebhooksConfig';

export const webhooksApi = {
  get() {
    return request<WebhooksConfig>(endpoint);
  },

  events() {
    return request<WebhookEventGroups>(`${endpoint}/events`);
  },

  update(input: UpdateWebhooksConfigInput) {
    return request<void>(endpoint, {
      method: 'PUT',
      body: input,
    });
  },
};
