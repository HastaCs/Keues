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
  IconLayoutGrid,
  IconListNumbers,
  IconScale,
  IconSearch,
  IconTable,
  IconTicket,
  IconTrash,
  IconAlertTriangle,
  IconUsers,
} from '@tabler/icons-react';

import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';

import { getErrorMessage } from '@/api/getErrorMessage';
import { useCounters } from '@/api/hooks/counters';
import { useCreateQueue, useQueues, useRemoveQueue, useUpdateQueue } from '@/api/hooks/queues';
import { Queue, QueueInput } from '@/api/interfaces/Queue/Queues';
import { EmptyState } from '@/components/EmptyState/EmptyState';
import { PageHeader } from '@/components/PageHeader/PageHeader';
import { useActiveLocation } from '@/features/locations/LocationContext';
import { QueueFormModal } from './QueueFormModal';

import styles from '@/styles/hover-card.module.css';

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
      const timeDiff = new Date(left.createdAt).getTime() - new Date(right.createdAt).getTime();

      if (timeDiff !== 0) {
        return timeDiff;
      }

      return left.name.localeCompare(right.name, 'es');
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

export function QueuesPanel() {
  const { t } = useTranslation();

  const location = useActiveLocation();

  const [searchParams, setSearchParams] = useSearchParams();

  const queuesQuery = useQueues(location?.id);
  const countersQuery = useCounters(location?.id);
  const createQueue = useCreateQueue(location?.id ?? '');
  const updateQueue = useUpdateQueue(location?.id ?? '');
  const removeQueue = useRemoveQueue(location?.id ?? '');

  const [search, setSearch] = useState('');

  const [sort, setSort] = useState<QueueSort>(getStoredSort);

  const [view, setView] = useState<QueueView>(getStoredView);

  const [formOpened, setFormOpened] = useState(false);

  const [editingQueue, setEditingQueue] = useState<Queue>();

  const [deletingQueue, setDeletingQueue] = useState<Queue>();

  const queues = queuesQuery.data ?? [];

  const counterMeta = useMemo(() => {
    const counters = countersQuery.data ?? [];

    return Object.fromEntries(
      counters.map((counter) => [counter.id, { name: counter.name, color: counter.color }])
    );
  }, [countersQuery.data]);

  const loadError = queuesQuery.isError
    ? getErrorMessage(queuesQuery.error, t('errors.unexpected'))
    : countersQuery.isError
      ? getErrorMessage(countersQuery.error, t('errors.unexpected'))
      : null;

  const mutationError = createQueue.error ?? updateQueue.error ?? removeQueue.error;
  const error = mutationError ? getErrorMessage(mutationError, t('errors.unexpected')) : loadError;

  useEffect(() => {
    const openId = searchParams.get('open');
    if (!openId || queues.length === 0) {
      return;
    }

    const match = queues.find((queue) => queue.id === openId);
    if (!match) {
      return;
    }

    setEditingQueue(match);
    setFormOpened(true);

    const next = new URLSearchParams(searchParams);
    next.delete('open');
    setSearchParams(next, { replace: true });
  }, [queues, searchParams, setSearchParams]);

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
    const closeForm = () => {
      setFormOpened(false);
      setEditingQueue(undefined);
    };

    if (editingQueue) {
      updateQueue.mutate({ ...payload, id: editingQueue.id }, { onSuccess: closeForm });
    } else {
      createQueue.mutate(payload, { onSuccess: closeForm });
    }
  }

  function handleConfirmDeleteQueue() {
    if (!deletingQueue) {
      return;
    }

    removeQueue.mutate(deletingQueue.id, {
      onSuccess: () => setDeletingQueue(undefined),
    });
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
        loading={createQueue.isPending || updateQueue.isPending}
        locationId={location.id}
        onClose={() => setFormOpened(false)}
        onSubmit={handleSubmitQueue}
      />

      <Stack gap="lg">
        <PageHeader
          label={location.name}
          title={t('ticketTypes.heading')}
          actions={
            queues.length > 0 ? (
              <Button onClick={openCreateModal}>{t('ticketTypes.newTicketType')}</Button>
            ) : undefined
          }
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

        {queuesQuery.isPending ? (
          <Group justify="center">
            <Loader />
          </Group>
        ) : filteredTicketTypes.length === 0 ? (
          <EmptyState
            title={t('ticketTypes.emptyTitle')}
            description={t('ticketTypes.emptyDescription')}
            action={
              queues.length === 0 ? (
                <Button onClick={openCreateModal}>{t('ticketTypes.newTicketType')}</Button>
              ) : undefined
            }
          />
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

                <Group gap={4} wrap="nowrap" mt="sm">
                  <Tooltip label={t('queueForm.priority')} withArrow>
                    <Badge
                      size="sm"
                      variant="light"
                      color="blue"
                      leftSection={<IconListNumbers size={12} />}
                    >
                      {ticketType.priority}
                    </Badge>
                  </Tooltip>

                  <Tooltip label={t('queueForm.weight')} withArrow>
                    <Badge
                      size="sm"
                      variant="light"
                      color="grape"
                      leftSection={<IconScale size={12} />}
                    >
                      {ticketType.weight}
                    </Badge>
                  </Tooltip>
                </Group>

                <Divider mt="sm" />

                <Group gap="xs" wrap="nowrap" align="center" mt="sm">
                  <IconUsers size={14} stroke={1.5} style={{ flexShrink: 0 }} />

                  {ticketType.counters.length === 0 ? (
                    <Group gap={4} wrap="nowrap">
                      <IconAlertTriangle
                        size={14}
                        color="var(--mantine-color-yellow-6)"
                        style={{ flexShrink: 0 }}
                      />
                      <Text size="xs" c="dimmed">
                        {t('ticketTypes.noCounters')}
                      </Text>
                    </Group>
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
                        <Group gap={4} wrap="nowrap">
                          <IconAlertTriangle
                            size={14}
                            color="var(--mantine-color-yellow-6)"
                            style={{ flexShrink: 0 }}
                          />
                          <Text size="xs" c="dimmed">
                            {t('ticketTypes.noCounters')}
                          </Text>
                        </Group>
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
