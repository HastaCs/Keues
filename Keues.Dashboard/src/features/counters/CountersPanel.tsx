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
  IconSearch,
  IconTable,
  IconTicket,
  IconTrash,
  IconUsers,
} from '@tabler/icons-react';
import { countersApi } from '@/api/CountersApi';
import { queuesApi } from '@/api/QueuesApi';
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/api/httpClient';

import styles from '@/styles/hover-card.module.css';

import { CounterFormModal } from './CounterFormModal';
import { Counter, CreateCounterInput } from '@/api/interfaces/Counter/Counters';
import { useActiveLocation } from '@/features/locations/LocationContext';
import { PageHeader } from '@/components/PageHeader/PageHeader';

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

type CounterView = 'cards' | 'table';

type CounterSortField = 'createdAt' | 'name' | 'code';

interface CounterSort {
  field: CounterSortField;
  direction: 'asc' | 'desc';
}

const SORT_STORAGE_KEY = 'keues.counters.sort';

const VIEW_STORAGE_KEY = 'keues.counters.view';

function getStoredSort(): CounterSort {
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

function persistSort(sort: CounterSort) {
  window.localStorage.setItem(SORT_STORAGE_KEY, `${sort.field}:${sort.direction}`);
}

function getStoredView(): CounterView {
  const stored = window.localStorage.getItem(VIEW_STORAGE_KEY);
  return stored === 'cards' || stored === 'table' ? stored : 'cards';
}

function sortCounters(items: Counter[], sort: CounterSort): Counter[] {
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
  field: CounterSortField;
  sort: CounterSort;
  onSort: (field: CounterSortField) => void;
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

interface QueueMeta {
  name: string;
  color: string;
}

export function CountersPanel() {
  const { t } = useTranslation();

  const location = useActiveLocation();

  const [counters, setCounters] = useState<Counter[]>([]);

  const [loading, setLoading] = useState(true);

  const [saving, setSaving] = useState(false);

  const [error, setError] = useState<string | null>(null);

  const [search, setSearch] = useState('');

  const [sort, setSort] = useState<CounterSort>(getStoredSort);

  const [view, setView] = useState<CounterView>(getStoredView);

  const [queueMeta, setQueueMeta] = useState<Record<string, QueueMeta>>({});

  const [formOpened, setFormOpened] = useState(false);

  const [editingCounter, setEditingCounter] = useState<Counter | undefined>();

  const [deletingCounter, setDeletingCounter] = useState<Counter | undefined>();

  const refreshCounters = useCallback(async () => {
    if (!location) {
      return;
    }

    setError(null);
    setLoading(true);

    try {
      const response = await countersApi.list(location.id);

      setCounters(response.data);

      const queuesResponse = await queuesApi.list(location.id);

      setQueueMeta(
        Object.fromEntries(
          queuesResponse.data.map((queue) => [
            queue.id,
            { name: queue.name, color: queue.color },
          ])
        )
      );
    } catch (requestError) {
      setError(getErrorMessage(requestError, t('errors.unexpected')));
    } finally {
      setLoading(false);
    }
  }, [location]);

  useEffect(() => {
    void refreshCounters();
  }, [refreshCounters]);

  const filteredCounters = useMemo(() => {
    const query = search.trim().toLowerCase();

    const matches = query.length
      ? counters.filter((counter) => {
          return (
            counter.code.toLowerCase().includes(query) ||
            counter.name.toLowerCase().includes(query) ||
            counter.description.toLowerCase().includes(query)
          );
        })
      : counters;

    return sortCounters(matches, sort);
  }, [counters, search, sort]);

  function handleSort(field: CounterSortField) {
    setSort((previous) => {
      const next: CounterSort =
        previous.field === field
          ? { field, direction: previous.direction === 'asc' ? 'desc' : 'asc' }
          : { field, direction: 'asc' };

      persistSort(next);

      return next;
    });
  }

  function openCreateModal() {
    setEditingCounter(undefined);

    setFormOpened(true);
  }

  function openEditModal(counter: Counter) {
    setEditingCounter(counter);

    setFormOpened(true);
  }

  async function handleSubmitCounter(payload: CreateCounterInput) {
    if (!location) {
      return;
    }

    setSaving(true);
    setError(null);

    try {
      if (editingCounter) {
        await countersApi.update({ id: editingCounter.id, ...payload });
      } else {
        await countersApi.create(payload);
      }

      setFormOpened(false);

      setEditingCounter(undefined);

      await refreshCounters();
    } catch (requestError) {
      setError(getErrorMessage(requestError, t('errors.unexpected')));
    } finally {
      setSaving(false);
    }
  }

  async function handleConfirmDeleteCounter() {
    if (!deletingCounter) {
      return;
    }

    setError(null);

    try {
      await countersApi.remove(deletingCounter.id);

      setDeletingCounter(undefined);

      await refreshCounters();
    } catch (requestError) {
      setError(getErrorMessage(requestError, t('errors.unexpected')));
    }
  }

  if (!location) {
    return null;
  }

  return (
    <>
      <Modal
        opened={Boolean(deletingCounter)}
        onClose={() => setDeletingCounter(undefined)}
        title={t('counters.deleteTitle')}
        centered
      >
        <Stack gap="lg">
          <Text>
            {t('counters.deleteDescription', {
              code: deletingCounter?.code ?? '',
            })}
          </Text>

          <Group justify="flex-end">
            <Button variant="default" onClick={() => setDeletingCounter(undefined)}>
              {t('common.cancel')}
            </Button>

            <Button color="red" onClick={handleConfirmDeleteCounter}>
              {t('common.delete')}
            </Button>
          </Group>
        </Stack>
      </Modal>

      <CounterFormModal
        opened={formOpened}
        initialCounter={editingCounter}
        loading={saving}
        locationId={location.id}
        onClose={() => setFormOpened(false)}
        onSubmit={handleSubmitCounter}
      />

      <Stack gap="lg">
        <PageHeader
          label={t('counters.title')}
          title={location.name}
          description={t('counters.subtitle')}
          actions={<Button onClick={openCreateModal}>{t('counters.newCounter')}</Button>}
        />

        {error && (
          <Alert color="red" title={t('errors.requestFailed')}>
            {error}
          </Alert>
        )}

        <Group align="flex-end" justify="space-between">
          <TextInput
            value={search}
            onChange={(event) => setSearch(event.currentTarget.value)}
            placeholder={t('counters.searchPlaceholder')}
            leftSection={<IconSearch size={16} />}
            style={{
              maxWidth: 420,
              width: '100%',
            }}
          />

          <Group gap="sm" align="flex-end">
            <SegmentedControl
              value={view}
              onChange={(value) => {
                const next = value as CounterView;
                setView(next);
                window.localStorage.setItem(VIEW_STORAGE_KEY, next);
              }}
              data={[
                {
                  value: 'cards',
                  label: (
                    <Group gap={6} wrap="nowrap">
                      <IconLayoutGrid size={14} />
                      {t('counters.viewCards')}
                    </Group>
                  ),
                },
                {
                  value: 'table',
                  label: (
                    <Group gap={6} wrap="nowrap">
                      <IconTable size={14} />
                      {t('counters.viewTable')}
                    </Group>
                  ),
                },
              ]}
            />

            <Select
              value={`${sort.field}:${sort.direction}`}
              onChange={(value) => {
                const next: CounterSort =
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
                  label: t('counters.sortCreated'),
                },
                {
                  value: 'name:asc',
                  label: t('counters.sortNameAZ'),
                },
                {
                  value: 'name:desc',
                  label: t('counters.sortNameZA'),
                },
              ]}
              allowDeselect={false}
              style={{
                minWidth: 220,
              }}
            />
          </Group>
        </Group>

        {loading ? (
          <Group justify="center" py="xl">
            <Loader />
          </Group>
        ) : filteredCounters.length === 0 ? (
          <Paper withBorder radius="md" p="xl">
            <Stack align="center">
              <Text fw={600}>{t('counters.emptyTitle')}</Text>

              <Text c="dimmed" ta="center">
                {t('counters.emptyDescription')}
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
            {filteredCounters.map((counter) => (
              <Card
                key={counter.id}
                withBorder
                radius="lg"
                p="md"
                className={styles.hoverCard}
                onClick={() => openEditModal(counter)}
                style={{
                  borderLeft: `6px solid var(--mantine-color-${counter.color}-6)`,
                  cursor: 'pointer',
                }}
              >
                <Group justify="space-between" align="flex-start" wrap="nowrap">
                  <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
                    <ThemeIcon size={36} radius="xl" color={counter.color} variant="light">
                      <IconUsers size={18} />
                    </ThemeIcon>

                    <Stack gap={1} style={{ minWidth: 0 }}>
                      <Text fw={700} truncate>
                        {counter.name}
                      </Text>

                      <Group gap={4} wrap="nowrap" style={{ minWidth: 0 }}>
                        <Tooltip label={t('counters.codeMonitorHelp')} withArrow>
                          <IconDeviceTv size={13} />
                        </Tooltip>
                        <Text size="xs" c="dimmed" truncate>
                          {counter.code}
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
                          setDeletingCounter(counter);
                        }}
                      >
                        <IconTrash size={18} />
                      </ActionIcon>
                    </Tooltip>
                  </Group>
                </Group>

                <Text size="sm" c="dimmed" lineClamp={2} mt="xs">
                  {counter.description || t('counters.noDescription')}
                </Text>

                <Divider mt="sm" />

                <Group gap="xs" wrap="nowrap" align="flex-start" mt="sm">
                  <IconTicket size={14} stroke={1.5} style={{ flexShrink: 0 }} />

                  {counter.queues && counter.queues.length === 0 ? (
                    <Text size="xs" c="dimmed">
                      {t('counters.noQueues')}
                    </Text>
                  ) : (
                    <Group gap={4} wrap="wrap">
                      {counter.queues?.map((queueId) => {
                        const meta = queueMeta[queueId];

                        return (
                          <Badge key={queueId} size="xs" variant="light" color={meta?.color}>
                            {meta?.name ?? queueId}
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
                    {t('counters.name')}
                  </SortableTh>
                  <SortableTh field="code" sort={sort} onSort={handleSort}>
                    {t('counters.codeMonitor')}
                  </SortableTh>
                  <Table.Th>{t('counters.description')}</Table.Th>
                  <Table.Th>{t('counters.queues')}</Table.Th>
                  <SortableTh field="createdAt" sort={sort} onSort={handleSort}>
                    {t('counters.createdAt')}
                  </SortableTh>
                  <Table.Th>{t('counters.actions')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {filteredCounters.map((counter) => (
                  <Table.Tr
                    key={counter.id}
                    style={{ cursor: 'pointer' }}
                    onClick={() => openEditModal(counter)}
                  >
                    <Table.Td>
                      <Group gap="xs" wrap="nowrap" style={{ minWidth: 0 }}>
                        <ThemeIcon size={28} radius="xl" color={counter.color} variant="light">
                          <IconUsers size={14} />
                        </ThemeIcon>
                        <Text fw={600} truncate>
                          {counter.name}
                        </Text>
                      </Group>
                    </Table.Td>
                    <Table.Td>
                      <Group gap={4} wrap="nowrap" style={{ minWidth: 0 }}>
                        <Tooltip label={t('counters.codeMonitorHelp')} withArrow>
                          <IconDeviceTv size={13} />
                        </Tooltip>
                        <Text size="sm" fw={500}>
                          {counter.code}
                        </Text>
                      </Group>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed" lineClamp={2}>
                        {counter.description || t('counters.noDescription')}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      {counter.queues && counter.queues.length === 0 ? (
                        <Text size="xs" c="dimmed">
                          {t('counters.noQueues')}
                        </Text>
                      ) : (
                        <Group gap={4} wrap="wrap">
                          {counter.queues?.map((queueId) => {
                            const meta = queueMeta[queueId];

                            return (
                              <Badge key={queueId} size="xs" variant="light" color={meta?.color}>
                                {meta?.name ?? queueId}
                              </Badge>
                            );
                          })}
                        </Group>
                      )}
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed">
                        {new Date(counter.createdAt).toLocaleDateString()}
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
                              openEditModal(counter);
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
                              setDeletingCounter(counter);
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
