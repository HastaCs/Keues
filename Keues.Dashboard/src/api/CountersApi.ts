import { request } from "./httpClient";
import type { ApiResponse } from "./interfaces/common/ApiResponse";
import { Counter, CreateCounterInput, UpdateCounterInput } from "./interfaces/Counter/Counters";

const endpoint = "/counters";

export const countersApi = {
  list(locationId: string) {
    return request<ApiResponse<Counter[]>>(`${endpoint}?locationId=${locationId}`);
  },
  
  get(id: string) {
    return request<Counter>(`${endpoint}/${id}`);
  },
  
  create(input: CreateCounterInput) {
    return request<Counter>(endpoint, {
      method: "POST",
      body: input,
    });
  },
  
  update(input: UpdateCounterInput) {
    return request<Counter>(`${endpoint}/${input.id}`, {
      method: "PUT",
      body: input,
    });
  },
  
  remove(id: string) {
    return request<void>(`${endpoint}/${id}`, {
      method: "DELETE",
    });
  },
};  