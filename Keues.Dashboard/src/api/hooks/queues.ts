import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { QueueId, QueueInput, UpdateQueueInput } from '../interfaces/Queue/Queues';
import { queuesApi } from '../QueuesApi';
import { queryKeys } from '../queryKeys';

export function useQueues(locationId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.queues(locationId ?? ''),
    queryFn: () => queuesApi.list(locationId as string),
    enabled: locationId !== undefined,
    select: (response) => response.data,
  });
}

export function useCreateQueue(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: QueueInput) => queuesApi.create(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.queues(locationId) }),
  });
}

export function useUpdateQueue(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: UpdateQueueInput) => queuesApi.update(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.queues(locationId) }),
  });
}

export function useRemoveQueue(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: QueueId) => queuesApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.queues(locationId) }),
  });
}
