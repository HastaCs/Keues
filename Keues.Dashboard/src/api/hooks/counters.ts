import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { CreateCounterInput, UpdateCounterInput } from '../interfaces/Counter/Counters';
import { countersApi } from '../CountersApi';
import { queryKeys } from '../queryKeys';

export function useCounters(locationId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.counters(locationId ?? ''),
    queryFn: () => countersApi.list(locationId as string),
    enabled: locationId !== undefined,
    select: (response) => response.data,
  });
}

export function useCreateCounter(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: CreateCounterInput) => countersApi.create(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.counters(locationId) }),
  });
}

export function useUpdateCounter(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: UpdateCounterInput) => countersApi.update(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.counters(locationId) }),
  });
}

export function useRemoveCounter(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => countersApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.counters(locationId) }),
  });
}
