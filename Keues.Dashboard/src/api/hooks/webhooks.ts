import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { UpdateWebhooksConfigInput } from '../interfaces/Webhooks/Webhooks';
import { webhooksApi } from '../WebhooksApi';
import { queryKeys } from '../queryKeys';

export function useWebhooksConfig(enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.webhooks(),
    queryFn: () => webhooksApi.get(),
    enabled,
  });
}

export function useWebhookEvents(enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.webhookEvents(),
    queryFn: () => webhooksApi.events(),
    enabled,
  });
}

export function useUpdateWebhooksConfig() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: UpdateWebhooksConfigInput) => webhooksApi.update(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.webhooks() }),
  });
}
