import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type {
  LocationId,
  LocationInput,
  UpdateLocationInput,
} from '../interfaces/Location/Locations';
import { locationsApi } from '../LocationsApi';
import { queryKeys } from '../queryKeys';

export function useLocations() {
  return useQuery({
    queryKey: queryKeys.locations(),
    queryFn: () => locationsApi.list(),
    select: (response) => response.data,
  });
}

export function useLocationDetail(locationId: LocationId | undefined) {
  return useQuery({
    queryKey: queryKeys.location(locationId ?? ''),
    queryFn: () => locationsApi.get(locationId as LocationId),
    enabled: locationId !== undefined,
  });
}

export function useCreateLocation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: LocationInput) => locationsApi.create(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.locations() }),
  });
}

export function useUpdateLocation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: UpdateLocationInput) => locationsApi.update(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.locations() }),
  });
}

export function useRemoveLocation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: LocationId) => locationsApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.locations() }),
  });
}
