import { keepPreviousData, useQuery } from '@tanstack/react-query';
import type { ListTicketsParams } from '../interfaces/Tickets/Tickets';
import { ticketsApi } from '../TicketsApi';
import { queryKeys } from '../queryKeys';

export function useTickets(params: ListTicketsParams, enabled = true) {
  return useQuery({
    queryKey: queryKeys.tickets(params.locationId, params),
    queryFn: () => ticketsApi.list(params),
    enabled,
    placeholderData: keepPreviousData,
  });
}

export function useTicketHistory(ticketId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.ticketHistory(ticketId ?? ''),
    queryFn: () => ticketsApi.getHistory(ticketId as string),
    enabled: ticketId !== undefined,
    select: (response) => response.data,
  });
}
