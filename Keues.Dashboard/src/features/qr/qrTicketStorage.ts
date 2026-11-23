const STORAGE_KEY = 'keues-qr-ticket';
const FINISHED_STORAGE_KEY = 'keues-qr-finished';

export interface StoredQrTicket {
  id: string;
  code: string;
  flowId: string;
  flowName?: string;
  createdAt: string;
}

type StoredQrTicketMap = Record<string, StoredQrTicket>;

function readMap(storageKey: string): StoredQrTicketMap {
  const raw = window.localStorage.getItem(storageKey);

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

function writeMap(storageKey: string, map: StoredQrTicketMap) {
  window.localStorage.setItem(storageKey, JSON.stringify(map));
}

export function getStoredQrTicket(flowId: string): StoredQrTicket | null {
  return readMap(STORAGE_KEY)[flowId] ?? null;
}

export function saveQrTicket(ticket: StoredQrTicket): void {
  const map = readMap(STORAGE_KEY);
  map[ticket.flowId] = ticket;
  writeMap(STORAGE_KEY, map);
}

export function clearQrTicket(flowId: string): void {
  const map = readMap(STORAGE_KEY);
  delete map[flowId];
  writeMap(STORAGE_KEY, map);
}

export function getFinishedQrTicket(flowId: string): StoredQrTicket | null {
  return readMap(FINISHED_STORAGE_KEY)[flowId] ?? null;
}

export function saveFinishedQrTicket(ticket: StoredQrTicket): void {
  const map = readMap(FINISHED_STORAGE_KEY);
  map[ticket.flowId] = ticket;
  writeMap(FINISHED_STORAGE_KEY, map);
}

export function clearFinishedQrTicket(flowId: string): void {
  const map = readMap(FINISHED_STORAGE_KEY);
  delete map[flowId];
  writeMap(FINISHED_STORAGE_KEY, map);
}
