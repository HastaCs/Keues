import { Alert, Badge, Card, Container, Group, Stack, Text, Title } from '@mantine/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { buildManualMonitorHubUrl } from '@/features/flows/manualMonitorUrl';

interface CalledTurn {
  code: string;
  counterCode: string;
  calledAt: number;
}

interface TicketCalledEvent {
  ticketCode?: string;
  counterCode?: string;
}

type MonitorStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api';

function createMonitorId(locationId: string, flowId: string): string {
  const storageKey = `keues-monitor-web:${locationId}:${flowId}`;
  const stored = localStorage.getItem(storageKey);
  if (stored) {
    return stored;
  }

  const id =
    typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function'
      ? crypto.randomUUID()
      : `web-${Math.random().toString(36).slice(2)}`;
  localStorage.setItem(storageKey, id);
  return id;
}

export function ManualFlowMonitorPage() {
  const { t } = useTranslation();
  const searchParams = useMemo(() => new URLSearchParams(window.location.search), []);
  const locationId = searchParams.get('locationId') ?? '';
  const flowId = searchParams.get('flowId') ?? '';
  const [status, setStatus] = useState<MonitorStatus>('connecting');
  const [turns, setTurns] = useState<CalledTurn[]>([]);

  const isValid = Boolean(locationId && flowId);

  useEffect(() => {
    if (!isValid) {
      setStatus('disconnected');
      return;
    }

    const deviceId = createMonitorId(locationId, flowId);
    const hubUrl = buildManualMonitorHubUrl(window.location.href, API_BASE_URL, {
      deviceId,
      name: 'Monitor web',
      locationId,
      flowId,
    });

    const connection: HubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .build();

    connection.on('TicketCalled', (event: TicketCalledEvent) => {
      const ticketCode = event.ticketCode;
      if (!ticketCode) {
        return;
      }

      setTurns((current) => [
        {
          code: ticketCode,
          counterCode: event.counterCode ?? '—',
          calledAt: Date.now(),
        },
        ...current.slice(0, 14),
      ]);
    });

    connection.onreconnecting(() => setStatus('reconnecting'));
    connection.onreconnected(() => setStatus('connected'));
    connection.onclose(() => setStatus('disconnected'));

    setStatus('connecting');
    void connection
      .start()
      .then(() => setStatus('connected'))
      .catch(() => setStatus('disconnected'));

    return () => {
      void connection.stop();
    };
  }, [flowId, isValid, locationId]);

  const statusColor: Record<MonitorStatus, string> = {
    connecting: 'yellow',
    connected: 'green',
    reconnecting: 'orange',
    disconnected: 'red',
  };

  if (!isValid) {
    return (
      <Container py={40} size="sm">
        <Alert color="red">{t('flows.manualMonitorInvalidUrl')}</Alert>
      </Container>
    );
  }

  return (
    <Container py={24} size="sm">
      <Stack gap="md">
        <Group justify="space-between">
          <div>
            <Title order={2}>{t('flows.manualMonitorPageTitle')}</Title>
            <Text c="dimmed">{t('flows.manualMonitorPageSubtitle')}</Text>
          </div>

          <Badge color={statusColor[status]} variant="light">
            {t(`flows.manualMonitorStatus.${status}`)}
          </Badge>
        </Group>

        <Card withBorder radius="md" p="xl">
          <Stack gap={6}>
            <Text size="sm" c="dimmed">
              {t('flows.manualMonitorLatest')}
            </Text>
            <Title order={1}>{turns[0]?.code ?? t('flows.manualMonitorWaiting')}</Title>
            <Text size="sm" c="dimmed">
              {turns[0]?.counterCode
                ? t('flows.manualMonitorCounter', { counter: turns[0].counterCode })
                : ''}
            </Text>
          </Stack>
        </Card>

        <Card withBorder radius="md" p="md">
          <Stack gap="xs">
            <Text fw={600}>{t('flows.manualMonitorHistory')}</Text>
            {turns.length === 0 ? (
              <Text c="dimmed">{t('flows.manualMonitorNoCalls')}</Text>
            ) : (
              turns.map((turn, index) => (
                <Group key={`${turn.code}-${turn.calledAt}-${index}`} justify="space-between">
                  <Text fw={index === 0 ? 700 : 500}>{turn.code}</Text>
                  <Text size="sm" c="dimmed">
                    {turn.counterCode}
                  </Text>
                </Group>
              ))
            )}
          </Stack>
        </Card>
      </Stack>
    </Container>
  );
}
