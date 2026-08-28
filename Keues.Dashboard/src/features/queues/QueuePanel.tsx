import {
  ActionIcon,
  Alert,
  Badge,
  Button,
  Card,
  Divider,
  Group,
  Loader,
  Modal,
  Paper,
  SegmentedControl,
  Select,
  SimpleGrid,
  Stack,
  Table,
  Text,
  TextInput,
  ThemeIcon,
  Tooltip,
} from '@mantine/core';

import {
  IconArrowsSort,
  IconChevronDown,
  IconChevronUp,
  IconDeviceTv,
  IconEdit,
  IconHash,
  IconHourglass,
  IconLayoutGrid,
  IconListNumbers,
  IconScale,
  IconSearch,
  IconTable,
  IconTicket,
  IconTrash,
  IconUsers,
} from '@tabler/icons-react';

import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

import { ApiError } from '@/api/httpClient';
import { queuesApi } from '@/api/QueuesApi';
import { countersApi } from '@/api/CountersApi';
import { Queue, QueueInput } from '@/api/interfaces/Queue/Queues';
import { useActiveLocation } from '@/features/locations/LocationContext';
import { QueueFormModal } from './QueueFormModal';
import { PageHeader } from '@/components/PageHeader/PageHeader';

import styles from '@/styles/hover-card.module.css';

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

type QueueView = 'cards' | 'table';

type QueueSortField = 'createdAt' | 'name' | 'code';

interface QueueSort {
  field: QueueSortField;
  direction: 'asc' | 'desc';
}

const SORT_STORAGE_KEY = 'keues.queues.sort';

const VIEW_STORAGE_KEY = 'keues.queues.view';

function getStoredSort(): QueueSort {
  const stored = window.localStorage.getItem(SORT_STORAGE_KEY);

  if (stored) {
    const [field, direction] = stored.split(':');

    if (
      (field === 'createdAt' || field === 'name' || field === 'code') &&
      (direction === 'asc' || direction === 'desc')
    ) {
      return { field, direction };
    }

    if (stored === 'asc') {
      return { field: 'name', direction: 'asc' };
    }

    if (stored === 'desc') {
      return { field: 'name', direction: 'desc' };
    }
  }

  return { field: 'createdAt', direction: 'asc' };
}

function persistSort(sort: QueueSort) {
  window.localStorage.setItem(SORT_STORAGE_KEY, `${sort.field}:${sort.direction}`);
}

function getStoredView(): QueueView {
  const stored = window.localStorage.getItem(VIEW_STORAGE_KEY);
  return stored === 'cards' || stored === 'table' ? stored : 'cards';
}

function sortQueues(items: Queue[], sort: QueueSort): Queue[] {
  const sorted = [...items].sort((left, right) => {
    if (sort.field === 'createdAt') {
      return new Date(left.createdAt).getTime() - new Date(right.createdAt).getTime();
    }

    return left[sort.field].localeCompare(right[sort.field], 'es');
  });

  return sort.direction === 'desc' ? sorted.reverse() : sorted;
}

function SortableTh({
  field,
  sort,
  onSort,
  children,
}: {
  field: QueueSortField;
  sort: QueueSort;
  onSort: (field: QueueSortField) => void;
  children: ReactNode;
}) {
  const active = sort.field === field;

  return (
    <Table.Th>
      <Group
        gap={4}
        wrap="nowrap"
        onClick={() => onSort(field)}
        style={{ cursor: 'pointer', userSelect: 'none' }}
      >
        {children}
        {active ? (
          sort.direction === 'asc' ? (
            <IconChevronUp size={14} />
          ) : (
            <IconChevronDown size={14} />
          )
        ) : (
          <IconArrowsSort size={14} style={{ opacity: 0.35 }} />
        )}
      </Group>
    </Table.Th>
  );
}

function getDisplayExample(displayCode: string) {
  return `${displayCode}001`;
}

interface CounterMeta {
  name: string;
  color: string;
}

export function QueuesPanel() {
  const { t } = useTranslation();

  const location = useActiveLocation();

  const [queues, setQueues] = useState<Queue[]>([]);

  const [loading, setLoading] = useState(true);

  const [saving, setSaving] = useState(false);

  const [error, setError] = useState<string | null>(null);

  const [search, setSearch] = useState('');

  const [sort, setSort] = useState<QueueSort>(getStoredSort);

  const [view, setView] = useState<QueueView>(getStoredView);

  const [counterMeta, setCounterMeta] = useState<Record<string, CounterMeta>>({});

  const [formOpened, setFormOpened] = useState(false);

  const [editingQueue, setEditingQueue] = useState<Queue>();

  const [deletingQueue, setDeletingQueue] = useState<Queue>();

  const refreshQueues = useCallback(async () => {
    if (!location) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await queuesApi.list(location.id);

      setQueues(response.data);

      const countersResponse = await countersApi.list(location.id);

      setCounterMeta(
        Object.fromEntries(
          countersResponse.data.map((counter) => [
            counter.id,
            { name: counter.name, color: counter.color },
          ])
        )
      );
    } catch (err) {
      setError(getErrorMessage(err, t('errors.unexpected')));
    } finally {
      setLoading(false);
    }
  }, [location]);

  useEffect(() => {
    void refreshQueues();
  }, [refreshQueues]);

  const filteredTicketTypes = useMemo(() => {
    const query = search.trim().toLowerCase();

    const filtered = query
      ? queues.filter(
          (item) =>
            item.code.toLowerCase().includes(query) ||
            item.name.toLowerCase().includes(query) ||
            item.description.toLowerCase().includes(query)
        )
      : queues;

    return sortQueues(filtered, sort);
  }, [queues, search, sort]);

  function handleSort(field: QueueSortField) {
    setSort((previous) => {
      const next: QueueSort =
        previous.field === field
          ? { field, direction: previous.direction === 'asc' ? 'desc' : 'asc' }
          : { field, direction: 'asc' };

      persistSort(next);

      return next;
    });
  }

  function openCreateModal() {
    setEditingQueue(undefined);
    setFormOpened(true);
  }

  function openEditModal(queue: Queue) {
    setEditingQueue(queue);
    setFormOpened(true);
  }

  async function handleSubmitQueue(payload: QueueInput) {
    setSaving(true);

    try {
      if (editingQueue) {
        await queuesApi.update({
          ...payload,
          id: editingQueue.id,
        });
      } else {
        await queuesApi.create(payload);
      }

      setFormOpened(false);
      setEditingQueue(undefined);

      await refreshQueues();
    } catch (err) {
      setError(getErrorMessage(err, t('errors.unexpected')));
    } finally {
      setSaving(false);
    }
  }

  async function handleConfirmDeleteQueue() {
    if (!deletingQueue) {
      return;
    }

    try {
      await queuesApi.remove(deletingQueue.id);

      setDeletingQueue(undefined);

      await refreshQueues();
    } catch (err) {
      setError(getErrorMessage(err, t('errors.unexpected')));
    }
  }

  if (!location) {
    return null;
  }

  return (
    <>
      <Modal
        opened={Boolean(deletingQueue)}
        onClose={() => setDeletingQueue(undefined)}
        title={t('ticketTypes.deleteTitle')}
        centered
      >
        <Stack gap="lg">
          <Text>
            {t('ticketTypes.deleteDescription', {
              name: deletingQueue?.name ?? '',
            })}
          </Text>

          <Group justify="flex-end">
            <Button variant="default" onClick={() => setDeletingQueue(undefined)}>
              {t('common.cancel')}
            </Button>

            <Button color="red" onClick={handleConfirmDeleteQueue}>
              {t('common.delete')}
            </Button>
          </Group>
        </Stack>
      </Modal>

      <QueueFormModal
        opened={formOpened}
        initialQueue={editingQueue}
        loading={saving}
        locationId={location.id}
        onClose={() => setFormOpened(false)}
        onSubmit={handleSubmitQueue}
      />

      <Stack gap="lg">
        <PageHeader
          label={t('ticketTypes.title')}
          title={location.name}
          description={t('ticketTypes.subtitle')}
          actions={<Button onClick={openCreateModal}>{t('ticketTypes.newTicketType')}</Button>}
        />

        {error && <Alert color="red">{error}</Alert>}

        <Group justify="space-between">
          <TextInput
            value={search}
            onChange={(e) => setSearch(e.currentTarget.value)}
            placeholder={t('ticketTypes.searchPlaceholder')}
            leftSection={<IconSearch size={16} />}
            style={{ maxWidth: 420, width: '100%' }}
          />

          <Group gap="sm" align="flex-end">
            <SegmentedControl
              value={view}
              onChange={(value) => {
                const next = value as QueueView;
                setView(next);
                window.localStorage.setItem(VIEW_STORAGE_KEY, next);
              }}
              data={[
                {
                  value: 'cards',
                  label: (
                    <Group gap={6} wrap="nowrap">
                      <IconLayoutGrid size={14} />
                      {t('ticketTypes.viewCards')}
                    </Group>
                  ),
                },
                {
                  value: 'table',
                  label: (
                    <Group gap={6} wrap="nowrap">
                      <IconTable size={14} />
                      {t('ticketTypes.viewTable')}
                    </Group>
                  ),
                },
              ]}
            />

            <Select
              value={`${sort.field}:${sort.direction}`}
              onChange={(value) => {
                const next: QueueSort =
                  value === 'name:asc'
                    ? { field: 'name', direction: 'asc' }
                    : value === 'name:desc'
                      ? { field: 'name', direction: 'desc' }
                      : { field: 'createdAt', direction: 'asc' };
                setSort(next);
                persistSort(next);
              }}
              data={[
                {
                  value: 'createdAt:asc',
                  label: t('ticketTypes.sortCreated'),
                },
                {
                  value: 'name:asc',
                  label: t('ticketTypes.sortNameAZ'),
                },
                {
                  value: 'name:desc',
                  label: t('ticketTypes.sortNameZA'),
                },
              ]}
              allowDeselect={false}
              style={{ minWidth: 220 }}
            />
          </Group>
        </Group>

        {loading ? (
          <Group justify="center">
            <Loader />
          </Group>
        ) : filteredTicketTypes.length === 0 ? (
          <Paper withBorder radius="md" p="xl">
            <Stack align="center">
              <Text fw={600}>{t('ticketTypes.emptyTitle')}</Text>

              <Text c="dimmed" ta="center">
                {t('ticketTypes.emptyDescription')}
              </Text>
            </Stack>
          </Paper>
        ) : view === 'cards' ? (
          <SimpleGrid
            cols={{
              base: 1,
              sm: 2,
              md: 3,
              xl: 4,
            }}
            spacing="md"
          >
            {filteredTicketTypes.map((ticketType) => (
              <Card
                key={ticketType.id}
                withBorder
                radius="lg"
                p="md"
                className={styles.hoverCard}
                onClick={() => openEditModal(ticketType)}
                style={{
                  borderLeft: `6px solid var(--mantine-color-${ticketType.color}-6)`,
                  cursor: 'pointer',
                }}
              >
                <Group justify="space-between" align="flex-start" wrap="nowrap">
                  <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
                    <ThemeIcon size={36} radius="xl" color={ticketType.color} variant="light">
                      <IconTicket size={18} />
                    </ThemeIcon>

                    <Stack gap={1} style={{ minWidth: 0 }}>
                      <Text fw={700} truncate>
                        {ticketType.name}
                      </Text>

                      <Group gap={4} wrap="nowrap" style={{ minWidth: 0 }}>
                        <Tooltip label={t('ticketTypes.prefixMonitorHelp')} withArrow>
                          <IconDeviceTv size={13} />
                        </Tooltip>

                        <Text size="xs" c="dimmed" truncate>
                          {ticketType.code}
                        </Text>
                      </Group>
                    </Stack>
                  </Group>

                  <Group gap={2}>
                    <Tooltip label={t('common.delete')}>
                      <ActionIcon
                        variant="light"
                        color="red"
                        size="md"
                        onClick={(event) => {
                          event.stopPropagation();
                          setDeletingQueue(ticketType);
                        }}
                      >
                        <IconTrash size={18} />
                      </ActionIcon>
                    </Tooltip>
                  </Group>
                </Group>

                <Text size="sm" c="dimmed" lineClamp={2} mt="xs">
                  {ticketType.description || t('ticketTypes.noDescription')}
                </Text>

                <Group gap="lg" wrap="wrap" mt="sm">
                  <Group gap={6} wrap="nowrap">
                    <IconTicket size={14} stroke={1.5} color="var(--mantine-color-dimmed)" />

                    <Text size="xs" c="dimmed">
                      {t('ticketTypes.exampleTicket')}
                    </Text>

                    <Text size="sm" fw={600} ff="monospace">
                      {getDisplayExample(ticketType.code)}
                    </Text>
                  </Group>

                  <Group gap={6} wrap="nowrap">
                    <IconHash size={14} stroke={1.5} color="var(--mantine-color-dimmed)" />

                    <Text size="xs" c="dimmed">
                      {t('ticketTypes.maxValue')}
                    </Text>

                    <Text size="sm" fw={600}>
                      {ticketType.maxValue ?? t('ticketTypes.noMaxValue')}
                    </Text>
                  </Group>
                </Group>

                <Paper bg="var(--mantine-color-gray-light)" radius="md" p="xs" mt="xs">
                  <Group gap="lg" wrap="wrap">
                    <Group gap={6} wrap="nowrap">
                      <IconListNumbers size={14} stroke={1.5} color="var(--mantine-color-dimmed)" />

                      <Text size="xs" c="dimmed">
                        {t('queueForm.priority')}
                      </Text>

                      <Text size="sm" fw={600}>
                        {ticketType.priority}
                      </Text>
                    </Group>

                    <Group gap={6} wrap="nowrap">
                      <IconScale size={14} stroke={1.5} color="var(--mantine-color-dimmed)" />

                      <Text size="xs" c="dimmed">
                        {t('queueForm.weight')}
                      </Text>

                      <Text size="sm" fw={600}>
                        {ticketType.weight}
                      </Text>
                    </Group>

                    <Group gap={6} wrap="nowrap">
                      <IconHourglass size={14} stroke={1.5} color="var(--mantine-color-dimmed)" />

                      <Text size="xs" c="dimmed">
                        {t('ticketTypes.aging')}
                      </Text>

                      <Text size="sm" fw={600}>
                        {ticketType.agingIntervalMinutes === 0
                          ? t('ticketTypes.disabled')
                          : `${ticketType.agingIntervalMinutes} min`}
                      </Text>
                    </Group>
                  </Group>
                </Paper>

                <Divider mt="sm" />

                <Group gap="xs" wrap="nowrap" align="center" mt="sm">
                  <IconUsers size={14} stroke={1.5} style={{ flexShrink: 0 }} />

                  {ticketType.counters.length === 0 ? (
                    <Text size="xs" c="dimmed">
                      {t('ticketTypes.noCounters')}
                    </Text>
                  ) : (
                    <Group gap={4} wrap="wrap">
                      {ticketType.counters.map((counterId) => {
                        const meta = counterMeta[counterId];

                        return (
                          <Badge key={counterId} size="xs" variant="light" color={meta?.color}>
                            {meta?.name ?? counterId}
                          </Badge>
                        );
                      })}
                    </Group>
                  )}
                </Group>
              </Card>
            ))}
          </SimpleGrid>
        ) : (
          <Paper withBorder radius="md" p="sm" style={{ overflowX: 'auto' }}>
            <Table highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <SortableTh field="name" sort={sort} onSort={handleSort}>
                    {t('ticketTypes.name')}
                  </SortableTh>
                  <SortableTh field="code" sort={sort} onSort={handleSort}>
                    {t('ticketTypes.displayCode')}
                  </SortableTh>
                  <Table.Th>{t('ticketTypes.description')}</Table.Th>
                  <Table.Th>{t('ticketTypes.maxValue')}</Table.Th>
                  <Table.Th>{t('ticketTypes.aging')}</Table.Th>
                  <Table.Th>{t('ticketTypes.counters')}</Table.Th>
                  <SortableTh field="createdAt" sort={sort} onSort={handleSort}>
                    {t('ticketTypes.createdAt')}
                  </SortableTh>
                  <Table.Th>{t('ticketTypes.actions')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {filteredTicketTypes.map((ticketType) => (
                  <Table.Tr
                    key={ticketType.id}
                    style={{ cursor: 'pointer' }}
                    onClick={() => openEditModal(ticketType)}
                  >
                    <Table.Td>
                      <Group gap="xs" wrap="nowrap" style={{ minWidth: 0 }}>
                        <ThemeIcon size={28} radius="xl" color={ticketType.color} variant="light">
                          <IconTicket size={14} />
                        </ThemeIcon>
                        <Text fw={600} truncate>
                          {ticketType.name}
                        </Text>
                      </Group>
                    </Table.Td>
                    <Table.Td>
                      <Group gap={4} wrap="nowrap" style={{ minWidth: 0 }}>
                        <Tooltip label={t('ticketTypes.prefixMonitorHelp')} withArrow>
                          <IconDeviceTv size={13} />
                        </Tooltip>
                        <Text size="sm" fw={500}>
                          {ticketType.code}
                        </Text>
                      </Group>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed" lineClamp={2}>
                        {ticketType.description || t('ticketTypes.noDescription')}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{ticketType.maxValue ?? t('ticketTypes.noMaxValue')}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">
                        {ticketType.agingIntervalMinutes === 0
                          ? t('ticketTypes.disabled')
                          : `${ticketType.agingIntervalMinutes} min`}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      {ticketType.counters.length === 0 ? (
                        <Text size="xs" c="dimmed">
                          {t('ticketTypes.noCounters')}
                        </Text>
                      ) : (
                        <Group gap={4} wrap="wrap">
                          {ticketType.counters.map((counterId) => {
                            const meta = counterMeta[counterId];

                            return (
                              <Badge key={counterId} size="xs" variant="light" color={meta?.color}>
                                {meta?.name ?? counterId}
                              </Badge>
                            );
                          })}
                        </Group>
                      )}
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed">
                        {new Date(ticketType.createdAt).toLocaleDateString()}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Group gap={2} wrap="nowrap">
                        <Tooltip label={t('common.edit')}>
                          <ActionIcon
                            variant="light"
                            size="md"
                            onClick={(event) => {
                              event.stopPropagation();
                              openEditModal(ticketType);
                            }}
                          >
                            <IconEdit size={16} />
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label={t('common.delete')}>
                          <ActionIcon
                            variant="light"
                            color="red"
                            size="md"
                            onClick={(event) => {
                              event.stopPropagation();
                              setDeletingQueue(ticketType);
                            }}
                          >
                            <IconTrash size={16} />
                          </ActionIcon>
                        </Tooltip>
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </Paper>
        )}
      </Stack>
    </>
  );
}
