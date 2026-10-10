import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { Flow, FlowInput, UpdateFlowInput } from '../interfaces/Flow/Flows';
import { flowsApi } from '../FlowsApi';
import { queryKeys } from '../queryKeys';

export function useFlows(locationId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.flows(locationId ?? ''),
    queryFn: async () => {
      const response = await flowsApi.list(locationId as string);
      return response.data as Flow[];
    },
    enabled: locationId !== undefined,
    refetchOnWindowFocus: false,
  });
}

export function useCreateFlow(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: FlowInput) => flowsApi.create(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.flows(locationId) }),
  });
}

export function useUpdateFlow(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: UpdateFlowInput) => flowsApi.update(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.flows(locationId) }),
  });
}

export function useRemoveFlow(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => flowsApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.flows(locationId) }),
  });
}
