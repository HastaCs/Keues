import {
  ActionIcon,
  Alert,
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

import { IconEdit, IconHash, IconSearch, IconTicket, IconTrash } from '@tabler/icons-react';

import { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { ApiError } from '@/api/httpClient';
import { queuesApi } from '@/api/QueuesApi';
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

function sortQueues(items: Queue[], direction: string): Queue[] {
  const sorted = [...items].sort((a, b) => a.code.localeCompare(b.code, 'es'));

  return direction === 'asc' ? sorted : sorted.reverse();
}

function getDisplayExample(displayCode: string) {
  return `${displayCode}001`;
}

export function QueuesPanel() {
  const { t } = useTranslation();

  const location = useActiveLocation();

  const [queues, setQueues] = useState<Queue[]>([]);

  const [loading, setLoading] = useState(true);

  const [saving, setSaving] = useState(false);

  const [error, setError] = useState<string | null>(null);

  const [search, setSearch] = useState('');

  const [sortDirection, setSortDirection] = useState<string>('asc');

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

    return sortQueues(filtered, sortDirection);
  }, [queues, search, sortDirection]);

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

          <Select
            value={sortDirection}
            onChange={(v) => setSortDirection((v as string) ?? 'asc')}
            data={[
              {
                value: 'asc',
                label: t('ticketTypes.sortDisplayCodeAZ'),
              },
              {
                value: 'desc',
                label: t('ticketTypes.sortDisplayCodeZA'),
              },
            ]}
            allowDeselect={false}
            style={{ minWidth: 220 }}
          />
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
        ) : (
          <SimpleGrid
            cols={{
              base: 1,
              sm: 2,
              lg: 3,
            }}
            spacing="md"
          >
            {filteredTicketTypes.map((ticketType) => (
              <Card
                key={ticketType.id}
                withBorder
                radius="lg"
                p="lg"
                className={styles.hoverCard}
                style={{
                  borderLeft: `6px solid var(--mantine-color-${ticketType.color}-6)`,
                }}
              >
                <Stack gap="md">
                  <Group justify="space-between" align="flex-start">
                    <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
                      <ThemeIcon size={44} radius="xl" color={ticketType.color} variant="light">
                        <IconTicket size={22} />
                      </ThemeIcon>

                      <Stack gap={2} style={{ minWidth: 0 }}>
                        <Text fw={700} truncate>
                          {ticketType.code}
                        </Text>

                        <Text size="xs" c="dimmed" truncate>
                          {ticketType.name}
                        </Text>
                      </Stack>
                    </Group>

                    <Group gap={4}>
                      <Tooltip label={t('common.edit')}>
                        <ActionIcon
                          variant="light"
                          color="blue"
                          onClick={() => openEditModal(ticketType)}
                        >
                          <IconEdit size={16} />
                        </ActionIcon>
                      </Tooltip>

                      <Tooltip label={t('common.delete')}>
                        <ActionIcon
                          variant="light"
                          color="red"
                          onClick={() => setDeletingQueue(ticketType)}
                        >
                          <IconTrash size={16} />
                        </ActionIcon>
                      </Tooltip>
                    </Group>
                  </Group>

                  <Divider />

                  <Text size="sm" c="dimmed" lineClamp={2}>
                    {ticketType.description || t('ticketTypes.noDescription')}
                  </Text>

                  <Group gap="xs" wrap="nowrap" align="center">
                    <IconTicket size={17} stroke={1.5} style={{ flexShrink: 0 }} />

                    <Stack gap={0}>
                      <Text size="xs" c="dimmed">
                        {t('ticketTypes.exampleTicket')}
                      </Text>

                      <Text size="sm" ff="monospace">
                        {getDisplayExample(ticketType.code)}
                      </Text>
                    </Stack>
                  </Group>

                  <Group gap="xs" wrap="nowrap" align="center">
                    <IconHash size={17} stroke={1.5} style={{ flexShrink: 0 }} />

                    <Stack gap={0}>
                      <Text size="xs" c="dimmed">
                        {t('ticketTypes.maxValue')}
                      </Text>

                      <Text size="sm">{ticketType.maxValue ?? t('ticketTypes.noMaxValue')}</Text>
                    </Stack>
                  </Group>
                </Stack>
              </Card>
            ))}
          </SimpleGrid>
        )}
      </Stack>
    </>
  );
}
