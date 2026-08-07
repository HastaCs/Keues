import { request } from "./httpClient";
import type { ApiResponse } from "./interfaces/common/ApiResponse";
import type { FlowInput, UpdateFlowInput, Flow } from "./interfaces/Flow/Flows";

import type { LocationId } from "@/api/interfaces/Location/Locations";

const endpoint = "/flows";

export const flowsApi = {
  list(locationId: LocationId) {
    return request<ApiResponse<any[]>>(`${endpoint}?locationId=${locationId}`).then((response) => ({
      ...response,
      data: response.data.map((flow) => ({
        ...flow,
        menuItems: JSON.parse(flow.flowJson ?? "[]"),
      })),
    }));
  },

  get(id: string) {
    return request<Flow>(`${endpoint}/${id}`);
  },

create(flow: FlowInput) {
  return request<Flow>(endpoint, {
    method: "POST",
    body: flow,
  }).then((response) => ({
    ...response,
    menuItems: JSON.parse(response.flowJson ?? "[]"),
  }));
},

update(flow: UpdateFlowInput) {
  return request<Flow>(`${endpoint}/${flow.id}`, {
    method: "PUT",
    body: flow,
  }).then((response) => ({
    ...response,
    menuItems: JSON.parse(response.flowJson ?? "[]"),
  }));
},

  remove(id: string) {
    return request<void>(`${endpoint}/${id}`, {
      method: "DELETE",
    });
  },
};
