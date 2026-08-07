import { request } from "./httpClient";

import type { ApiResponse } from "./interfaces/common/ApiResponse";
import { LocationId, LocationInput, LocationKeue, UpdateLocationInput } from "./interfaces/Location/Locations";

const endpoint = "/locations";

export const locationsApi = {
  list() {
    return request<ApiResponse<LocationKeue[]>>(endpoint);
  },

  get(id: LocationId) {
    return request<LocationKeue>(`${endpoint}/${id}`);
  },

  create(input: LocationInput) {
    return request<LocationKeue>(endpoint, {
      method: "POST",
      body: input,
    });
  },

  update(input: UpdateLocationInput) {
    return request<LocationKeue>(`${endpoint}/${input.id}`, {
      method: "PUT",
      body: input,
    });
  },

  remove(id: LocationId) {
    return request<void>(`${endpoint}/${id}`, {
      method: "DELETE",
    });
  },
};