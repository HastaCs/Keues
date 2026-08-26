import {
  Alert,
  Badge,
  Button,
  Group,
  Loader,
  Pagination,
  Paper,
  Select,
  Stack,
  Table,
  Text,
  TextInput,
  ThemeIcon,
} from '@mantine/core';
import { IconSearch, IconTicket, IconX } from '@tabler/icons-react';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/api/httpClient';
import { queuesApi } from '@/api/QueuesApi';
import { ticketsApi } from '@/api/TicketsApi';
import type { Queue } from '@/api/interfaces/Queue/Queues';
import { TICKET_STATUS, type Ticket, type TicketStatus } from '@/api/interfaces/Tickets/Tickets';
import { PageHeader } from '@/components/PageHeader/PageHeader';
import { useActiveLocation } from '@/features/locations/LocationContext';

type SortDirection = 'asc' | 'desc';
type StatusFilter = 'all' | TicketStatus;

const PAGE_SIZE = 20;

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

function toIsoDate(value: string, endOfDay: boolean): string | undefined {
  if (!value) {
    return undefined;
  }

  const parts = value.split('-').map(Number);
  if (parts.length !== 3 || parts.some((part) => Number.isNaN(part))) {
    return undefined;
  }

  if (endOfDay) {
    return new Date(Date.UTC(parts[0], parts[1] - 1, parts[2], 23, 59, 59, 999)).toISOString();
  }

  return new Date(Date.UTC(parts[0], parts[1] - 1, parts[2])).toISOString();
}

function getStatusColor(status: TicketStatus): string {
  if (status === TICKET_STATUS.Waiting) {
    return 'yellow';
  }

  if (status === TICKET_STATUS.InProgress) {
    return 'blue';
  }

  if (status === TICKET_STATUS.Attended) {
    return 'green';
  }

  return 'red';
}

const STATUS_LABEL_KEYS: Record<TicketStatus, string> = {
  [TICKET_STATUS.Waiting]: 'tickets.statusWaiting',
  [TICKET_STATUS.InProgress]: 'tickets.statusInProgress',
  [TICKET_STATUS.Attended]: 'tickets.statusAttended',
  [TICKET_STATUS.Canceled]: 'tickets.statusCanceled',
};

function formatDate(value: string | null): string {
  if (!value) {
    return '-';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '-';
  }

  return date.toLocaleString();
}

export function TicketsPanel() {
  const { t } = useTranslation();
  const location = useActiveLocation();

  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [queues, setQueues] = useState<Queue[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [queueFilter, setQueueFilter] = useState<string>('all');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE);
  const [pagination, setPagination] = useState<{ total: number; totalPages: number }>({
    total: 0,
    totalPages: 1,
  });

  useEffect(() => {
    if (!location) {
      return;
    }

    let cancelled = false;
    queuesApi
      .list(location.id)
      .then((response) => {
        if (!cancelled) {
          setQueues(response.data);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setQueues([]);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [location]);

  useEffect(() => {
    setPage(1);
  }, [statusFilter, queueFilter, dateFrom, dateTo, pageSize]);

  useEffect(() => {
    if (!location) {
      return;
    }

    let cancelled = false;

    setError(null);
    setLoading(true);

    ticketsApi
      .list({
        locationId: location.id,
        status: statusFilter === 'all' ? undefined : statusFilter,
        queueId: queueFilter === 'all' ? undefined : queueFilter,
        createdFrom: toIsoDate(dateFrom, false),
        createdTo: toIsoDate(dateTo, true),
        page,
        limit: pageSize,
        sortOrder: sortDirection,
      })
      .then((response) => {
        if (!cancelled) {
          setTickets(response.data);
          setPagination({
            total: response.pagination?.total ?? 0,
            totalPages: response.pagination?.totalPages ?? 1,
          });
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
  }, [location, statusFilter, queueFilter, dateFrom, dateTo, page, pageSize, sortDirection, t]);

  const filteredTickets = useMemo(() => {
    const query = search.trim().toLowerCase();

    const matches = tickets.filter((ticket) => {
      const matchesQuery =
        query.length === 0 ||
        ticket.code.toLowerCase().includes(query) ||
        (ticket.queue?.name ?? '').toLowerCase().includes(query) ||
        (ticket.counter?.name ?? '').toLowerCase().includes(query);

      return matchesQuery;
    });

    return matches;
  }, [tickets, search]);

  function handleClearFilters() {
    setSearch('');
    setStatusFilter('all');
    setQueueFilter('all');
    setDateFrom('');
    setDateTo('');
    setPage(1);
  }

  if (!location) {
    return null;
  }

  const hasActiveFilters =
    statusFilter !== 'all' || queueFilter !== 'all' || dateFrom !== '' || dateTo !== '';

  return (
    <Stack gap="lg">
      <PageHeader
        label={t('tickets.title')}
        title={location.name}
        description={t('tickets.subtitle')}
      />

      {error && (
        <Alert color="red" title={t('errors.requestFailed')}>
          {error}
        </Alert>
      )}

      <Group align="flex-end" wrap="wrap">
        <TextInput
          value={search}
          onChange={(event) => setSearch(event.currentTarget.value)}
          placeholder={t('tickets.searchPlaceholder')}
          leftSection={<IconSearch size={16} />}
          style={{ maxWidth: 420, width: '100%' }}
        />

        <Select
          value={String(statusFilter)}
          onChange={(value) =>
            setStatusFilter(value === 'all' ? 'all' : (Number(value) as TicketStatus))
          }
          data={[
            { value: 'all', label: t('tickets.statusAll') },
            { value: String(TICKET_STATUS.Waiting), label: t('tickets.statusWaiting') },
            { value: String(TICKET_STATUS.InProgress), label: t('tickets.statusInProgress') },
            { value: String(TICKET_STATUS.Attended), label: t('tickets.statusAttended') },
            { value: String(TICKET_STATUS.Canceled), label: t('tickets.statusCanceled') },
          ]}
          allowDeselect={false}
          style={{ minWidth: 170 }}
        />

        <Select
          value={queueFilter}
          onChange={(value) => setQueueFilter(value ?? 'all')}
          data={[
            { value: 'all', label: t('tickets.queueAll') },
            ...queues.map((queue) => ({ value: queue.id, label: queue.name })),
          ]}
          allowDeselect={false}
          style={{ minWidth: 170 }}
        />

        <Group align="flex-end" gap="md">
          <TextInput
            label={t('tickets.dateFrom')}
            type="date"
            value={dateFrom}
            onChange={(event) => setDateFrom(event.currentTarget.value)}
            style={{ width: 170 }}
          />

          <TextInput
            label={t('tickets.dateTo')}
            type="date"
            value={dateTo}
            onChange={(event) => setDateTo(event.currentTarget.value)}
            style={{ width: 170 }}
          />
        </Group>

        <Select
          value={sortDirection}
          onChange={(value) => setSortDirection((value as SortDirection) ?? 'desc')}
          data={[
            { value: 'desc', label: t('tickets.sortNewest') },
            { value: 'asc', label: t('tickets.sortOldest') },
          ]}
          allowDeselect={false}
          style={{ minWidth: 190 }}
        />

        {hasActiveFilters ? (
          <Button variant="default" leftSection={<IconX size={16} />} onClick={handleClearFilters}>
            {t('tickets.clearFilters')}
          </Button>
        ) : null}
      </Group>

      {loading ? (
        <Group justify="center" py="xl">
          <Loader />
        </Group>
      ) : filteredTickets.length === 0 ? (
        <Paper withBorder radius="md" p="xl">
          <Stack align="center" gap={6}>
            <Text fw={600}>{t('tickets.emptyTitle')}</Text>
            <Text c="dimmed" ta="center">
              {t('tickets.emptyDescription')}
            </Text>
          </Stack>
        </Paper>
      ) : (
        <Paper withBorder radius="md" p="sm">
          <Table highlightOnHover>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>{t('tickets.code')}</Table.Th>
                <Table.Th>{t('tickets.status')}</Table.Th>
                <Table.Th>{t('tickets.queue')}</Table.Th>
                <Table.Th>{t('tickets.counter')}</Table.Th>
                <Table.Th>{t('tickets.createdAt')}</Table.Th>
                <Table.Th>{t('tickets.calledAt')}</Table.Th>
                <Table.Th>{t('tickets.attendedAt')}</Table.Th>
                <Table.Th>{t('tickets.canceledAt')}</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {filteredTickets.map((ticket) => (
                <Table.Tr key={ticket.id}>
                  <Table.Td>
                    <Group gap="xs" wrap="nowrap">
                      <ThemeIcon variant="light" color="blue" size={24}>
                        <IconTicket size={14} />
                      </ThemeIcon>
                      <Text fw={700}>{ticket.code}</Text>
                    </Group>
                  </Table.Td>
                  <Table.Td>
                    <Badge variant="light" color={getStatusColor(ticket.status)}>
                      {t(STATUS_LABEL_KEYS[ticket.status])}
                    </Badge>
                  </Table.Td>
                  <Table.Td>
                    <Text>{ticket.queue?.name ?? '-'}</Text>
                  </Table.Td>
                  <Table.Td>
                    <Text>{ticket.counter?.name ?? '-'}</Text>
                  </Table.Td>
                  <Table.Td>
                    <Text>{formatDate(ticket.createdAt)}</Text>
                  </Table.Td>
                  <Table.Td>
                    <Text>{formatDate(ticket.calledAt)}</Text>
                  </Table.Td>
                  <Table.Td>
                    <Text>{formatDate(ticket.attendedAt)}</Text>
                  </Table.Td>
                  <Table.Td>
                    <Text>{formatDate(ticket.canceledAt)}</Text>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>

          <Group
            justify="space-between"
            align="center"
            wrap="wrap"
            gap="sm"
            px="sm"
            py="sm"
            style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}
          >
            <Text size="sm" c="dimmed">
              {t('tickets.showing', {
                from: pagination.total === 0 ? 0 : (page - 1) * pageSize + 1,
                to: Math.min(page * pageSize, pagination.total),
                total: pagination.total,
              })}
            </Text>

            <Group gap="xs">
              <Pagination total={pagination.totalPages} value={page} onChange={setPage} />
              <Select
                value={String(pageSize)}
                onChange={(value) => setPageSize(Number(value) || PAGE_SIZE)}
                data={[10, 20, 50, 100].map((size) => ({ value: String(size), label: `${size}` }))}
                aria-label={t('tickets.pageSize')}
                allowDeselect={false}
                style={{ width: 90 }}
              />
            </Group>
          </Group>
        </Paper>
      )}
    </Stack>
  );
}
