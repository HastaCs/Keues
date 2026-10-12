interface ManualMonitorUrlParams {
  locationId: string;
  flowId: string;
}

interface ManualMonitorHubUrlParams extends ManualMonitorUrlParams {
  deviceId: string;
  name: string;
}

export function buildManualMonitorUrl(baseHref: string, params: ManualMonitorUrlParams): string {
  const url = new URL('/monitor/manual', baseHref);
  url.searchParams.set('locationId', params.locationId);
  url.searchParams.set('flowId', params.flowId);
  return url.toString();
}

export function buildManualMonitorHubUrl(
  baseHref: string,
  apiBaseUrl: string,
  params: ManualMonitorHubUrlParams
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
