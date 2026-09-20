import {
  Alert,
  Avatar,
  Group,
  Loader,
  Modal,
  Stack,
  Text,
  ThemeIcon,
  Timeline,
  type MantineColor,
} from '@mantine/core';
import {
  IconArrowsExchange,
  IconBell,
  IconCheck,
  IconClock,
  IconDoorEnter,
  IconHistory,
  IconTicket,
  IconX,
} from '@tabler/icons-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/api/httpClient';
import { ticketsApi } from '@/api/TicketsApi';
import type { Ticket, TicketHistory } from '@/api/interfaces/Tickets/Tickets';

interface EventMeta {
  labelKey: string;
  color: MantineColor;
  icon: React.ComponentType<{ size?: number; stroke?: number }>;
}

const EVENT_META: Record<string, EventMeta> = {
  'Ticket.Created': { labelKey: 'tickets.historyCreated', color: 'blue', icon: IconTicket },
  'Ticket.Called': { labelKey: 'tickets.historyCalled', color: 'cyan', icon: IconBell },
  'Ticket.Attended': { labelKey: 'tickets.historyAttended', color: 'green', icon: IconCheck },
  'Ticket.Canceled': { labelKey: 'tickets.historyCanceled', color: 'red', icon: IconX },
  'Ticket.Transferred': {
    labelKey: 'tickets.historyTransferred',
    color: 'violet',
    icon: IconArrowsExchange,
  },
};

const DEFAULT_EVENT_META: EventMeta = {
  labelKey: 'tickets.historyEvent',
  color: 'gray',
  icon: IconHistory,
};

function getEventMeta(event: string): EventMeta {
  return EVENT_META[event] ?? DEFAULT_EVENT_META;
}

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);

  if (parts.length === 0) {
    return '?';
  }

  if (parts.length === 1) {
    return parts[0].charAt(0).toUpperCase();
  }

  return `${parts[0].charAt(0)}${parts[parts.length - 1].charAt(0)}`.toUpperCase();
}

function formatDate(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString();
}

interface TicketHistoryModalProps {
  ticket: Ticket | null;
  opened: boolean;
  onClose: () => void;
}

export function TicketHistoryModal({ ticket, opened, onClose }: TicketHistoryModalProps) {
  const { t } = useTranslation();
  const [history, setHistory] = useState<TicketHistory[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!opened || !ticket) {
      return;
    }

    let cancelled = false;

    setLoading(true);
    setError(null);

    ticketsApi
      .getHistory(ticket.id)
      .then((response) => {
        if (!cancelled) {
          setHistory(response.data);
        }
      })
      .catch((requestError) => {
        if (!cancelled) {
          setError(getErrorMessage(requestError, t('tickets.historyError')));
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
  }, [opened, ticket, t]);

  return (
    <Modal opened={opened} onClose={onClose} title={t('tickets.history')} centered size="lg">
      {loading ? (
        <Stack align="center" py="xl">
          <Loader />
        </Stack>
      ) : error ? (
        <Alert color="red" title={t('errors.requestFailed')}>
          {error}
        </Alert>
      ) : history.length === 0 ? (
        <Stack align="center" py="xl" gap={6}>
          <Text c="dimmed">{t('tickets.historyEmpty')}</Text>
        </Stack>
      ) : (
        <Timeline active={history.length - 1} bulletSize={30} lineWidth={3}>
          {history.map((entry) => {
            const meta = getEventMeta(entry.event);
            const EventIcon = meta.icon;

            return (
              <Timeline.Item
                key={entry.id}
                color={meta.color}
                title={
                  entry.queueName ? `${t(meta.labelKey)} → ${entry.queueName}` : t(meta.labelKey)
                }
                bullet={<EventIcon size={14} />}
              >
                <Stack gap={8} mt={4}>
                  <Group gap={6} wrap="nowrap" c="dimmed">
                    <IconClock size={14} />
                    <Text size="xs">{formatDate(entry.createdAt)}</Text>
                  </Group>

                  {entry.user || entry.counterName ? (
                    <Group gap="xl" wrap="wrap">
                      {entry.user ? (
                        <Group gap={8} wrap="nowrap">
                          <Avatar size={30} radius="xl" color={meta.color}>
                            {getInitials(entry.user.name)}
                          </Avatar>
                          <Stack gap={0}>
                            <Text size="10px" c="dimmed" tt="uppercase" fw={600} lh={1.2}>
                              {t('tickets.historyUser')}
                            </Text>
                            <Text size="sm" fw={600} lh={1.3}>
                              {entry.user.name}
                            </Text>
                          </Stack>
                        </Group>
                      ) : null}

                      {entry.counterName ? (
                        <Group gap={8} wrap="nowrap">
                          <ThemeIcon size={30} radius="xl" variant="light" color="gray">
                            <IconDoorEnter size={16} />
                          </ThemeIcon>
                          <Stack gap={0}>
                            <Text size="10px" c="dimmed" tt="uppercase" fw={600} lh={1.2}>
                              {t('tickets.counter')}
                            </Text>
                            <Text size="sm" fw={600} lh={1.3}>
                              {entry.counterName}
                            </Text>
                          </Stack>
                        </Group>
                      ) : null}
                    </Group>
                  ) : null}
                </Stack>
              </Timeline.Item>
            );
          })}
        </Timeline>
      )}
    </Modal>
  );
}
