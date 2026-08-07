export interface DashboardLocation {
  id: string;
  name: string;
  description: string | null;
  color: string;
}

export interface TicketsTodaySummary {
  total: number;
  waiting: number;
  inProgress: number;
  attended: number;
  canceled: number;
}

export interface NowServingItem {
  counterId: string;
  counterName: string;
  counterCode: string;
  counterColor: string;
  ticketId: string;
  ticketCode: string;
  queueId: string;
  queueName: string;
  calledAt: string | null;
}

export interface WaitingTicketItem {
  ticketId: string;
  ticketCode: string;
  queueId: string;
  queueName: string;
  queueColor: string;
  createdAt: string;
  waitingMinutes: number;
}

export interface DashboardSummary {
  location: DashboardLocation;
  counters: number;
  queues: number;
  ticketsToday: TicketsTodaySummary;
  averageWaitMinutes: number | null;
  averageServiceMinutes: number | null;
  nowServing: NowServingItem[];
  waitingTickets: WaitingTicketItem[];
}

export interface DashboardParams {
  locationId: string;
  date?: string;
}
