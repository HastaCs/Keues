interface MonitorHubUrlParams {
  deviceId: string;
  name: string;
  locationId: string;
  flowId: string;
}

export function buildMonitorHubUrl(
  baseHref: string,
  apiBaseUrl: string,
  params: MonitorHubUrlParams
): string {
  const apiUrl = new URL(apiBaseUrl, baseHref);
  const hubUrl = new URL('/devices', apiUrl.origin);
  hubUrl.searchParams.set('deviceId', params.deviceId);
  hubUrl.searchParams.set('name', params.name);
  hubUrl.searchParams.set('locationId', params.locationId);
  hubUrl.searchParams.set('flowId', params.flowId);
  hubUrl.searchParams.set('type', 'Monitor');
  return hubUrl.toString();
}
