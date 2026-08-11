import {
  Alert,
  Badge,
  Box,
  Card,
  Group,
  Loader,
  Paper,
  SimpleGrid,
  Stack,
  Table,
  Text,
  ThemeIcon,
} from '@mantine/core';
import {
  IconArrowRight,
  IconClock,
  IconHourglass,
  IconList,
  IconLogin2,
  IconStack2,
  IconUsers,
} from '@tabler/icons-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/api/httpClient';
import { dashboardApi } from '@/api/DashboardApi';
import type { DashboardSummary } from '@/api/interfaces/Dashboard/Dashboard';
import { PageHeader } from '@/components/PageHeader/PageHeader';
import { useActiveLocation } from '@/features/locations/LocationContext';

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

function formatMinutes(value: number | null): string {
  if (value === null || Number.isNaN(value)) {
    return '-';
  }

  return `${Math.round(value)} min`;
}

function formatClock(value: string | null): string {
  if (!value) {
    return '-';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '-';
  }

  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function KpiCard({
  label,
  value,
  icon: Icon,
  color,
}: {
  label: string;
  value: number;
  icon: React.ComponentType<{ size?: number }>;
  color: string;
}) {
  return (
    <Card withBorder radius="md" padding="md">
      <Group gap="sm" wrap="nowrap" align="center">
        <ThemeIcon variant="light" color={color} size={44} radius="md">
          <Icon size={22} />
        </ThemeIcon>
        <Stack gap={0}>
          <Text size="xl" fw={800} lh={1.15}>
            {value}
          </Text>
          <Text size="sm" c="dimmed" lh={1.2}>
            {label}
          </Text>
        </Stack>
      </Group>
    </Card>
  );
}

function AvgCard({
  label,
  icon: Icon,
  value,
}: {
  label: string;
  icon: React.ComponentType<{ size?: number }>;
  value: number | null;
}) {
  return (
    <Card withBorder radius="md" padding="md">
      <Group gap="sm" wrap="nowrap" align="center">
        <ThemeIcon variant="light" color="teal" size={44} radius="md">
          <Icon size={22} />
        </ThemeIcon>
        <Stack gap={0}>
          <Text size="xl" fw={800} lh={1.15}>
            {formatMinutes(value)}
          </Text>
          <Text size="sm" c="dimmed" lh={1.2}>
            {label}
          </Text>
        </Stack>
      </Group>
    </Card>
  );
}

function WaitingTicketsTable({ items }: { items: DashboardSummary['waitingTickets'] }) {
  const { t } = useTranslation();

  if (items.length === 0) {
    return (
      <Text c="dimmed" size="sm">
        {t('dashboard.waitingByQueueEmpty')}
      </Text>
    );
  }

  return (
    <Table highlightOnHover>
      <Table.Thead>
        <Table.Tr>
          <Table.Th>{t('dashboard.waitingQueue')}</Table.Th>
          <Table.Th>{t('dashboard.waitingTicket')}</Table.Th>
          <Table.Th>{t('dashboard.waitingSince')}</Table.Th>
          <Table.Th>{t('dashboard.waitingMinutesLabel')}</Table.Th>
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {items.map((item) => (
          <Table.Tr key={item.ticketId}>
            <Table.Td>
              <Group gap="xs" wrap="nowrap">
                <Box
                  w={10}
                  h={10}
                  style={{
                    borderRadius: '50%',
                    backgroundColor: `var(--mantine-color-${item.queueColor}-6)`,
                  }}
                />
                <Text fw={600}>{item.queueName}</Text>
              </Group>
            </Table.Td>
            <Table.Td>
              <Badge variant="light" color="yellow">
                {item.ticketCode}
              </Badge>
            </Table.Td>
            <Table.Td>
              <Text>{formatClock(item.createdAt)}</Text>
            </Table.Td>
            <Table.Td>
              <Text>{formatMinutes(item.waitingMinutes)}</Text>
            </Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  );
}

function NowServingTable({ items }: { items: DashboardSummary['nowServing'] }) {
  const { t } = useTranslation();

  if (items.length === 0) {
    return (
      <Text c="dimmed" size="sm">
        {t('dashboard.nowServingEmpty')}
      </Text>
    );
  }

  return (
    <Table highlightOnHover>
      <Table.Thead>
        <Table.Tr>
          <Table.Th>{t('dashboard.servingCounter')}</Table.Th>
          <Table.Th>{t('dashboard.servingTicket')}</Table.Th>
          <Table.Th>{t('dashboard.servingQueue')}</Table.Th>
          <Table.Th>{t('dashboard.servingSince')}</Table.Th>
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {items.map((item) => (
          <Table.Tr key={item.ticketId}>
            <Table.Td>
              <Group gap="xs" wrap="nowrap">
                <ThemeIcon variant="light" color={item.counterColor} size={26} radius="sm">
                  <IconUsers size={14} />
                </ThemeIcon>
                <Text fw={600}>
                  {item.counterCode} · {item.counterName}
                </Text>
              </Group>
            </Table.Td>
            <Table.Td>
              <Badge variant="light" color="blue">
                {item.ticketCode}
              </Badge>
            </Table.Td>
            <Table.Td>
              <Text>{item.queueName}</Text>
            </Table.Td>
            <Table.Td>
              <Text>{formatClock(item.calledAt)}</Text>
            </Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  );
}

export function LocationDashboard() {
  const { t } = useTranslation();
  const location = useActiveLocation();

  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!location) {
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    dashboardApi
      .get({ locationId: location.id })
      .then((response) => {
        if (!cancelled) {
          setSummary(response);
        }
      })
      .catch((requestError) => {
        if (!cancelled) {
          setError(getErrorMessage(requestError, t('errors.unexpected')));
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [location, t]);

  if (!location) {
    return null;
  }

  const ticketsToday = summary?.ticketsToday ?? {
    total: 0,
    waiting: 0,
    inProgress: 0,
    attended: 0,
    canceled: 0,
  };

  return (
    <Stack gap="lg">
      <PageHeader
        label={t('dashboard.title')}
        title={location.name}
        description={location.description || t('counters.noDescription')}
      />

      {error && (
        <Alert color="red" title={t('errors.requestFailed')}>
          {error}
        </Alert>
      )}

      {loading || !summary ? (
        <Group justify="center" py="xl">
          <Loader />
        </Group>
      ) : (
        <>
          <SimpleGrid cols={{ base: 2, xs: 3, md: 6 }}>
            <KpiCard
              label={t('dashboard.kpiTotal')}
              value={ticketsToday.total}
              icon={IconStack2}
              color="gray"
            />
            <KpiCard
              label={t('dashboard.kpiWaiting')}
              value={ticketsToday.waiting}
              icon={IconHourglass}
              color="yellow"
            />
            <KpiCard
              label={t('dashboard.kpiInProgress')}
              value={ticketsToday.inProgress}
              icon={IconArrowRight}
              color="blue"
            />
            <KpiCard
              label={t('dashboard.kpiAttended')}
              value={ticketsToday.attended}
              icon={IconStack2}
              color="green"
            />
            <KpiCard
              label={t('dashboard.kpiCanceled')}
              value={ticketsToday.canceled}
              icon={IconClock}
              color="red"
            />
            <AvgCard
              label={t('dashboard.avgWait')}
              value={summary.averageWaitMinutes}
              icon={IconClock}
            />
          </SimpleGrid>

          <SimpleGrid cols={{ base: 1, md: 3 }}>
            <Card withBorder radius="md" padding="lg">
              <Stack gap="md">
                <Text fw={700}>{t('dashboard.overviewTitle')}</Text>
                <Group gap="xl">
                  <Group gap="xs">
                    <ThemeIcon variant="light" color="violet" size={40} radius="md">
                      <IconUsers size={20} />
                    </ThemeIcon>
                    <Stack gap={0}>
                      <Text size="xl" fw={800} lh={1.15}>
                        {summary.counters}
                      </Text>
                      <Text size="sm" c="dimmed">
                        {t('dashboard.countersLabel')}
                      </Text>
                    </Stack>
                  </Group>
                  <Group gap="xs">
                    <ThemeIcon variant="light" color="cyan" size={40} radius="md">
                      <IconList size={20} />
                    </ThemeIcon>
                    <Stack gap={0}>
                      <Text size="xl" fw={800} lh={1.15}>
                        {summary.queues}
                      </Text>
                      <Text size="sm" c="dimmed">
                        {t('dashboard.queuesLabel')}
                      </Text>
                    </Stack>
                  </Group>
                </Group>
              </Stack>
            </Card>

            <Card withBorder radius="md" padding="lg">
              <Stack gap="sm">
                <Text fw={700}>{t('dashboard.avgService')}</Text>
                <Group gap="xs">
                  <ThemeIcon variant="light" color="teal" size={40} radius="md">
                    <IconLogin2 size={20} />
                  </ThemeIcon>
                  <Text size="2xl" fw={800}>
                    {formatMinutes(summary.averageServiceMinutes)}
                  </Text>
                </Group>
              </Stack>
            </Card>
          </SimpleGrid>

          <SimpleGrid cols={{ base: 1, lg: 2 }}>
            <Paper withBorder radius="md" p="lg">
              <Stack gap="md">
                <Text fw={700}>{t('dashboard.nowServingTitle')}</Text>
                <NowServingTable items={summary.nowServing ?? []} />
              </Stack>
            </Paper>

            <Paper withBorder radius="md" p="lg">
              <Stack gap="md">
                <Text fw={700}>{t('dashboard.waitingByQueueTitle')}</Text>
                <WaitingTicketsTable items={summary.waitingTickets ?? []} />
              </Stack>
            </Paper>
          </SimpleGrid>
        </>
      )}
    </Stack>
  );
}
