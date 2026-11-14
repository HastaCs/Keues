import { request } from './httpClient';

const QR_VIRTUAL_FLOW_TYPE = 3;

export type QrFlowErrorCode = 'invalid-flow' | 'missing-queue';

export class QrFlowError extends Error {
  code: QrFlowErrorCode;

  constructor(code: QrFlowErrorCode) {
    super(code);
    this.name = 'QrFlowError';
    this.code = code;
  }
}

export interface QrTicket {
  id: string;
  code: string;
}

export interface QrIssueResult {
  ticket: QrTicket;
  flowName: string;
}

export const QR_TICKET_STATUS = {
  waiting: 0,
  inProgress: 1,
  attended: 2,
  canceled: 3,
} as const;

export interface QrTicketStatus {
  id: string;
  code: string;
  status: number;
  counterName: string | null;
}

interface TicketResponse {
  id: string;
  code: string;
  status: number;
  counter: { name: string } | null;
}

interface FlowResponse {
  name: string;
  flowType: number;
  flowJson: string;
}

interface FlowJsonNode {
  nodeType?: string;
  queueId?: string | null;
}

export function extractQrQueueId(flowJson: string): string | null {
  try {
    const parsed = JSON.parse(flowJson) as unknown;

    let items: unknown;
    if (Array.isArray(parsed)) {
      items = parsed;
    } else if (
      parsed &&
      typeof parsed === 'object' &&
      Array.isArray((parsed as { menuItems?: unknown }).menuItems)
    ) {
      items = (parsed as { menuItems: unknown[] }).menuItems;
    } else {
      return null;
    }

    for (const item of items as FlowJsonNode[]) {
      if (!item || typeof item !== 'object') {
        continue;
      }

      if (item.nodeType && item.nodeType !== 'ticket') {
        continue;
      }

      if (typeof item.queueId === 'string' && item.queueId.length > 0) {
        return item.queueId;
      }
    }
  } catch {
    return null;
  }

  return null;
}

export async function issueQrTicket(flowId: string, token: string): Promise<QrIssueResult> {
  const flow = await request<FlowResponse>(`/flows/${flowId}`);

  if (flow.flowType !== QR_VIRTUAL_FLOW_TYPE) {
    throw new QrFlowError('invalid-flow');
  }

  const queueId = extractQrQueueId(flow.flowJson);

  if (!queueId) {
    throw new QrFlowError('missing-queue');
  }

  const ticket = await request<QrTicket>(`/queues/${queueId}/qr-new-ticket`, {
    method: 'POST',
    body: { flowId, token },
  });

  return { ticket, flowName: flow.name };
}

export function isFinishedTicketStatus(status: number): boolean {
  return status === QR_TICKET_STATUS.attended || status === QR_TICKET_STATUS.canceled;
}

export async function getQrTicketStatus(ticketId: string): Promise<QrTicketStatus> {
  const ticket = await request<TicketResponse>(`/tickets/${ticketId}`);

  return {
    id: ticket.id,
    code: ticket.code,
    status: ticket.status,
    counterName: ticket.counter?.name ?? null,
  };
}
