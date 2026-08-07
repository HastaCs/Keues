import { IconTicket } from '@tabler/icons-react';
import { DevicesPanel } from '@/features/devices/DevicesPanel';

export function DeviceTicketMachinesPanel() {
  return <DevicesPanel ns="deviceTicketMachines" deviceType={0} icon={IconTicket} />;
}
