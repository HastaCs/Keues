import { request } from './httpClient';
import type { ApiResponse } from './interfaces/common/ApiResponse';
import type { Device } from './interfaces/Devices/Devices';
import { LocationId } from './interfaces/Location/Locations';

const endpoint = '/devices';

export const devicesApi = {
  listMachines(locationId: LocationId) {
    return request<ApiResponse<Device[]>>(`${endpoint}?deviceType=0&locationId=${locationId}`);
  },
  listCounters(locationId: LocationId) {
    return request<ApiResponse<Device[]>>(`${endpoint}?deviceType=1&locationId=${locationId}`);
  },
  listMonitors(locationId: LocationId) {
    return request<ApiResponse<Device[]>>(`${endpoint}?deviceType=2&locationId=${locationId}`);
  },
  remove(id: string) {
    return request<void>(`${endpoint}/${id}`, {
      method: 'DELETE',
    });
  },
};
