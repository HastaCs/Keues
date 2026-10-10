import { useQuery } from '@tanstack/react-query';
import { dashboardApi } from '../DashboardApi';
import { queryKeys } from '../queryKeys';

export function useDashboard(locationId: string | undefined, date?: string) {
  return useQuery({
    queryKey: queryKeys.dashboard(locationId ?? '', date),
    queryFn: () => dashboardApi.get({ locationId: locationId as string, date }),
    enabled: locationId !== undefined,
  });
}
