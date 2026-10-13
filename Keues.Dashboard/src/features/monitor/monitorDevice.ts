const DEVICE_ID_STORAGE_KEY = 'keues-monitor:deviceId';

function createUuid(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (char) => {
    const random = (Math.random() * 16) | 0;
    const value = char === 'x' ? random : (random & 0x3) | 0x8;
    return value.toString(16);
  });
}

export function getMonitorDeviceId(): string {
  const stored = window.localStorage.getItem(DEVICE_ID_STORAGE_KEY);
  if (stored) {
    return stored;
  }

  const id = createUuid();
  window.localStorage.setItem(DEVICE_ID_STORAGE_KEY, id);
  return id;
}
