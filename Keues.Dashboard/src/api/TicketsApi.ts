import { request } from "./httpClient";

import type { ApiResponse } from "./interfaces/common/ApiResponse";
import { ListTicketsParams, Ticket, TicketId } from "./interfaces/Tickets/Tickets";

const endpoint = "/tickets";

function buildQuery(params: ListTicketsParams): string {
  const query = new URLSearchParams();
  query.set("locationId", params.locationId);

  if (params.status !== undefined) {
    query.set("status", String(params.status));
  }

  if (params.queueId) {
    query.set("queueId", params.queueId);
  }

  if (params.createdFrom) {
    query.set("createdFrom", params.createdFrom);
  }

  if (params.createdTo) {
    query.set("createdTo", params.createdTo);
  }

  return query.toString();
}

export const ticketsApi = {
  list(params: ListTicketsParams) {
    return request<ApiResponse<Ticket[]>>(`${endpoint}?${buildQuery(params)}`);
  },

  get(id: TicketId) {
    return request<Ticket>(`${endpoint}/${id}`);
  },
};
