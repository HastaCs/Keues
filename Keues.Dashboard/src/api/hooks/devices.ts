import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { devicesApi } from '../DevicesApi';
import { queryKeys } from '../queryKeys';

export function useDevices(deviceType: number, locationId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.devices(deviceType, locationId ?? ''),
    queryFn: () => {
      if (deviceType === 0) {
        return devicesApi.listMachines(locationId as string);
      }

      if (deviceType === 1) {
        return devicesApi.listCounters(locationId as string);
      }

      return devicesApi.listMonitors(locationId as string);
    },
    enabled: locationId !== undefined,
    select: (response) => response.data ?? [],
  });
}

export function useRemoveDevice(deviceType: number, locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => devicesApi.remove(id),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: queryKeys.devices(deviceType, locationId) }),
  });
}
