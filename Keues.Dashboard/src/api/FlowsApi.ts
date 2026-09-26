import { request } from "./httpClient";
import type { ApiResponse } from "./interfaces/common/ApiResponse";
import type {
  FlowInput,
  FlowMenuItem,
  UpdateFlowInput,
  Flow,
} from "./interfaces/Flow/Flows";

import type { LocationId } from "@/api/interfaces/Location/Locations";

const endpoint = "/flows";

function parseMenuItems(flowJson: unknown): FlowMenuItem[] {
  if (typeof flowJson !== "string" || flowJson.trim() === "") {
    return [];
  }

  try {
    const parsed = JSON.parse(flowJson);

    if (Array.isArray(parsed)) {
      return parsed;
    }

    if (parsed && Array.isArray(parsed.menuItems)) {
      return parsed.menuItems;
    }

    return [];
  } catch {
    return [];
  }
}

export const flowsApi = {
  list(locationId: LocationId) {
    return request<ApiResponse<any[]>>(`${endpoint}?locationId=${locationId}`).then((response) => ({
      ...response,
      data: response.data.map((flow) => ({
        ...flow,
        menuItems: parseMenuItems(flow.flowJson),
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
    menuItems: parseMenuItems(response.flowJson),
  }));
},

update(flow: UpdateFlowInput) {
  return request<Flow>(`${endpoint}/${flow.id}`, {
    method: "PUT",
    body: flow,
  }).then((response) => ({
    ...response,
    menuItems: parseMenuItems(response.flowJson),
  }));
},

  remove(id: string) {
    return request<void>(`${endpoint}/${id}`, {
      method: "DELETE",
    });
  },
};
