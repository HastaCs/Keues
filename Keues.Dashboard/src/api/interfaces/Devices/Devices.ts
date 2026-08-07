export interface Device {
  id: string;
  name: string;
  type: number;
  isConnected: boolean;
  lastConnection: string | null;
  locationId: string;
}