import { IconUsers } from '@tabler/icons-react';
import { DevicesPanel } from '@/features/devices/DevicesPanel';

export function CounterDevicesPanel() {
  return <DevicesPanel ns="counterDevices" deviceType={1} icon={IconUsers} />;
}
