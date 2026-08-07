import { request } from "./httpClient";

import type { ApiResponse } from "./interfaces/common/ApiResponse";
import { Queue, QueueId, QueueInput, UpdateQueueInput } from "./interfaces/Queue/Queues";

const endpoint = "/queues";

export const queuesApi = {
  list(locationId: string) {
    return request<ApiResponse<Queue[]>>(`${endpoint}?locationId=${locationId}`);
  },

  get(id: QueueId) {
    return request<Queue>(`${endpoint}/${id}`);
  }
,
  create(input: QueueInput) {
    return request<Queue>(endpoint, {
      method: "POST",
      body: input,
    });
  },

  update(input: UpdateQueueInput) {
    return request<Queue>(`${endpoint}/${input.id}`, {
      method: "PUT",
      body: input,
    });
  },

  remove(id: QueueId) {
    return request<void>(`${endpoint}/${id}`, {
      method: "DELETE",
    });
  },
};