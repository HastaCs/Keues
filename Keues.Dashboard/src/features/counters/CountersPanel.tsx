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
  Select,
  SimpleGrid,
  Stack,
  Text,
  TextInput,
  ThemeIcon,
  Tooltip,
} from '@mantine/core';

import {
  IconDeviceTv,
  IconEdit,
  IconSearch,
  IconTicket,
  IconTrash,
  IconUsers,
} from '@tabler/icons-react';
import { countersApi } from '@/api/CountersApi';
import { queuesApi } from '@/api/QueuesApi';
import { useCallback, useEffect, useMemo, useState } from 'react';
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

type CounterSortMode = 'created' | 'asc' | 'desc';

const SORT_STORAGE_KEY = 'keues.counters.sort';

function getStoredSort(): CounterSortMode {
  const stored = window.localStorage.getItem(SORT_STORAGE_KEY);
  return stored === 'asc' || stored === 'desc' || stored === 'created' ? stored : 'created';
}

function sortCounters(items: Counter[], mode: CounterSortMode): Counter[] {
  if (mode === 'created') {
    return [...items].sort(
      (left, right) => new Date(left.createdAt).getTime() - new Date(right.createdAt).getTime()
    );
  }

  const sorted = [...items].sort((left, right) => left.name.localeCompare(right.name, 'es'));

  return mode === 'asc' ? sorted : sorted.reverse();
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

  const [sortDirection, setSortDirection] = useState<CounterSortMode>(getStoredSort);

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

    return sortCounters(matches, sortDirection);
  }, [counters, search, sortDirection]);

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

          <Select
            value={sortDirection}
            onChange={(value) => {
              const next = (value as CounterSortMode) ?? 'created';
              setSortDirection(next);
              window.localStorage.setItem(SORT_STORAGE_KEY, next);
            }}
            data={[
              {
                value: 'created',
                label: t('counters.sortCreated'),
              },
              {
                value: 'asc',
                label: t('counters.sortNameAZ'),
              },
              {
                value: 'desc',
                label: t('counters.sortNameZA'),
              },
            ]}
            allowDeselect={false}
            style={{
              minWidth: 220,
            }}
          />
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
        ) : (
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
        )}
      </Stack>
    </>
  );
}
