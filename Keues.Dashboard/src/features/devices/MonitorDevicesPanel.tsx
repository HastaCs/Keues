import { IconDeviceTv } from '@tabler/icons-react';
import { DevicesPanel } from '@/features/devices/DevicesPanel';

export function MonitorDevicesPanel() {
  return <DevicesPanel ns="monitorDevices" deviceType={2} icon={IconDeviceTv} />;
}
