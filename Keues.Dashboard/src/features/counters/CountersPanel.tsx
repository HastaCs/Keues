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

import { IconEdit, IconSearch, IconTrash, IconUsers } from '@tabler/icons-react';
import { countersApi } from '@/api/CountersApi';
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

function sortCounters(items: Counter[], direction: string): Counter[] {
  const sorted = [...items].sort((left, right) => left.code.localeCompare(right.code, 'es'));

  return direction === 'asc' ? sorted : sorted.reverse();
}

export function CountersPanel() {
  const { t } = useTranslation();

  const location = useActiveLocation();

  const [counters, setCounters] = useState<Counter[]>([]);

  const [loading, setLoading] = useState(true);

  const [saving, setSaving] = useState(false);

  const [error, setError] = useState<string | null>(null);

  const [search, setSearch] = useState('');

  const [sortDirection, setSortDirection] = useState<string>('asc');

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
            onChange={(value) => setSortDirection((value as string) ?? 'asc')}
            data={[
              {
                value: 'asc',
                label: t('counters.sortCodeAZ'),
              },
              {
                value: 'desc',
                label: t('counters.sortCodeZA'),
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
              lg: 3,
            }}
            spacing="md"
          >
            {filteredCounters.map((counter) => (
              <Card
                key={counter.id}
                withBorder
                radius="lg"
                p="lg"
                className={styles.hoverCard}
                style={{
                  borderLeft: `6px solid var(--mantine-color-${counter.color}-6)`,
                }}
              >
                <Stack gap="md">
                  <Group justify="space-between" align="flex-start">
                    <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
                      <ThemeIcon size={44} radius="xl" color={counter.color} variant="light">
                        <IconUsers size={22} />
                      </ThemeIcon>

                      <Stack gap={2} style={{ minWidth: 0 }}>
                        <Text fw={700} truncate>
                          {counter.code}
                        </Text>

                        <Text size="xs" c="dimmed" truncate>
                          {counter.name}
                        </Text>
                      </Stack>
                    </Group>

                    <Group gap={4}>
                      <Tooltip label={t('common.edit')}>
                        <ActionIcon
                          variant="light"
                          color="blue"
                          onClick={() => openEditModal(counter)}
                        >
                          <IconEdit size={16} />
                        </ActionIcon>
                      </Tooltip>

                      <Tooltip label={t('common.delete')}>
                        <ActionIcon
                          variant="light"
                          color="red"
                          onClick={() => setDeletingCounter(counter)}
                        >
                          <IconTrash size={16} />
                        </ActionIcon>
                      </Tooltip>
                    </Group>
                  </Group>

                  <Divider />

                  <Text size="sm" c="dimmed" lineClamp={2}>
                    {counter.description || t('counters.noDescription')}
                  </Text>

                  <Stack gap={6}>
                    <Text fw={600} size="sm">
                      {t('sidebar.ticketTypes')}
                    </Text>

                    {counter.queues && counter.queues.length === 0 ? (
                      <Text size="sm" c="dimmed">
                        {t('counters.noQueues')}
                      </Text>
                    ) : (
                      <Badge variant="light" color={counter.color}>
                        {t('counters.queueTypeCount', { count: counter.queues?.length ?? 0 })}
                      </Badge>
                    )}
                  </Stack>
                </Stack>
              </Card>
            ))}
          </SimpleGrid>
        )}
      </Stack>
    </>
  );
}
