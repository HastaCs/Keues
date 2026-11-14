const STORAGE_KEY = 'keues-qr-ticket';

export interface StoredQrTicket {
  id: string;
  code: string;
  flowId: string;
  flowName?: string;
  createdAt: string;
}

type StoredQrTicketMap = Record<string, StoredQrTicket>;

function readMap(): StoredQrTicketMap {
  const raw = window.localStorage.getItem(STORAGE_KEY);

  if (!raw) {
    return {};
  }

  try {
    const parsed = JSON.parse(raw) as unknown;

    if (!parsed || typeof parsed !== 'object') {
      return {};
    }

    return parsed as StoredQrTicketMap;
  } catch {
    return {};
  }
}

function writeMap(map: StoredQrTicketMap) {
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(map));
}

export function getStoredQrTicket(flowId: string): StoredQrTicket | null {
  return readMap()[flowId] ?? null;
}

export function saveQrTicket(ticket: StoredQrTicket): void {
  const map = readMap();
  map[ticket.flowId] = ticket;
  writeMap(map);
}

export function clearQrTicket(flowId: string): void {
  const map = readMap();
  delete map[flowId];
  writeMap(map);
}
