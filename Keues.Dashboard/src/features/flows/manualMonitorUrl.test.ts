import { describe, expect, it } from 'vitest';
import { buildMonitorHubUrl } from './manualMonitorUrl';

describe('manualMonitorUrl', () => {
  it('builds the SignalR hub URL from the API base URL', () => {
    const url = buildMonitorHubUrl(
      'https://dashboard.keues.dev/locations',
      'https://api.keues.dev/api',
      {
        locationId: 'loc-1',
        flowId: 'flow-1',
        deviceId: 'device-1',
        name: 'Monitor web',
      }
    );

    expect(url).toBe(
      'https://api.keues.dev/devices?deviceId=device-1&name=Monitor+web&locationId=loc-1&flowId=flow-1&type=Monitor'
    );
  });
});
