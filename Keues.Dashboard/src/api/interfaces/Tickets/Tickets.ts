export type TicketId = string;

export const TICKET_STATUS = {
  Waiting: 0,
  InProgress: 1,
  Attended: 2,
  Canceled: 3,
} as const;

export type TicketStatus = (typeof TICKET_STATUS)[keyof typeof TICKET_STATUS];

export interface TicketQueue {
  id: string;
  name: string;
}

export interface TicketCounter {
  id: string;
  name: string;
}

export interface Ticket {
  id: TicketId;
  code: string;
  status: TicketStatus;
  createdAt: string;
  calledAt: string | null;
  attendedAt: string | null;
  canceledAt: string | null;
  queue: TicketQueue | null;
  counter: TicketCounter | null;
}

export interface ListTicketsParams {
  locationId: string;
  status?: TicketStatus;
  queueId?: string;
  createdFrom?: string;
  createdTo?: string;
}
