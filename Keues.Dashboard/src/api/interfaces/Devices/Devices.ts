export interface Device {
  id: string;
  name: string;
  type: number;
  isConnected: boolean;
  isActive: boolean;
  lastConnection: string | null;
  locationId: string;
}
