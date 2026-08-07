import { request } from './httpClient';
import type { DashboardParams, DashboardSummary } from './interfaces/Dashboard/Dashboard';

const endpoint = '/dashboard';

function buildQuery(params: DashboardParams): string {
  const query = new URLSearchParams();
  query.set('locationId', params.locationId);

  if (params.date) {
    query.set('date', params.date);
  }

  return query.toString();
}

export const dashboardApi = {
  get(params: DashboardParams) {
    return request<DashboardSummary>(`${endpoint}?${buildQuery(params)}`);
  },
};
