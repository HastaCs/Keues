import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ApiResponse } from '../interfaces/common/ApiResponse';
import type { Device } from '../interfaces/Devices/Devices';
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

export function useSetDeviceActive(deviceType: number, locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (variables: { id: string; isActive: boolean }) =>
      devicesApi.setActive(variables.id, variables.isActive),
    onMutate: async (variables) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.devices(deviceType, locationId) });

      const previous = queryClient.getQueryData<ApiResponse<Device[]>>(
        queryKeys.devices(deviceType, locationId)
      );

      queryClient.setQueryData<ApiResponse<Device[]>>(
        queryKeys.devices(deviceType, locationId),
        (current) =>
          current
            ? {
                ...current,
                data: current.data.map((device) =>
                  device.id === variables.id ? { ...device, isActive: variables.isActive } : device
                ),
              }
            : current
      );

      return { previous };
    },
    onError: (_error, _variables, context) => {
      queryClient.setQueryData(queryKeys.devices(deviceType, locationId), context?.previous);
    },
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: queryKeys.devices(deviceType, locationId) }),
  });
}
