import { request } from './httpClient';

import type { ApiResponse } from './interfaces/common/ApiResponse';
import { ListTicketsParams, Ticket, TicketHistory, TicketId } from './interfaces/Tickets/Tickets';

const endpoint = '/tickets';

function buildQuery(params: ListTicketsParams): string {
  const query = new URLSearchParams();
  query.set('locationId', params.locationId);

  if (params.status !== undefined) {
    query.set('status', String(params.status));
  }

  if (params.queueId) {
    query.set('queueId', params.queueId);
  }

  if (params.createdFrom) {
    query.set('createdFrom', params.createdFrom);
  }

  if (params.createdTo) {
    query.set('createdTo', params.createdTo);
  }

  if (params.page !== undefined) {
    query.set('page', String(params.page));
  }

  if (params.limit !== undefined) {
    query.set('limit', String(params.limit));
  }

  if (params.sortOrder !== undefined) {
    query.set('sortOrder', params.sortOrder);
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

  getHistory(id: TicketId) {
    return request<ApiResponse<TicketHistory[]>>(`${endpoint}/${id}/history`);
  },
};
