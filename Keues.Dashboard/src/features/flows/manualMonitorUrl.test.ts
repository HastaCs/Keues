import { describe, expect, it } from 'vitest';
import { buildManualMonitorHubUrl, buildManualMonitorUrl } from './manualMonitorUrl';

describe('manualMonitorUrl', () => {
  it('builds the manual monitor page URL with location and flow', () => {
    const url = buildManualMonitorUrl('https://keues.dev/locations/abc/flows', {
      locationId: 'loc-1',
      flowId: 'flow-1',
    });

    expect(url).toBe('https://keues.dev/monitor/manual?locationId=loc-1&flowId=flow-1');
  });

  it('builds the SignalR hub URL from the API base URL', () => {
    const url = buildManualMonitorHubUrl('https://dashboard.keues.dev/locations', 'https://api.keues.dev/api', {
      locationId: 'loc-1',
      flowId: 'flow-1',
      deviceId: 'device-1',
      name: 'Monitor web',
    });

    expect(url).toBe(
      'https://api.keues.dev/devices?deviceId=device-1&name=Monitor+web&locationId=loc-1&flowId=flow-1&type=Monitor'
    );
  });
});
