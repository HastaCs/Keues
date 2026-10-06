import {
  Alert,
  Button,
  Group,
  Loader,
  Modal,
  Select,
  Stack,
  Switch,
  Tabs,
  Text,
  TextInput,
} from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/api/httpClient';
import type { WebhookEventGroups } from '@/api/interfaces/Webhooks/Webhooks';
import { webhooksApi } from '@/api/WebhooksApi';

interface SettingsModalProps {
  opened: boolean;
  onClose: () => void;
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

function groupLabel(group: string): string {
  const base = group.replace(/Events$/, '');
  return base.charAt(0).toUpperCase() + base.slice(1);
}

export function SettingsModal({ opened, onClose }: SettingsModalProps) {
  const { t, i18n } = useTranslation();

  const [url, setUrl] = useState('');
  const [key, setKey] = useState('');
  const [enabled, setEnabled] = useState(false);
  const [eventGroups, setEventGroups] = useState<WebhookEventGroups>({});
  const [selectedEvents, setSelectedEvents] = useState<string[]>([]);

  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!opened) {
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    Promise.all([webhooksApi.get(), webhooksApi.events()])
      .then(([config, events]) => {
        if (cancelled) {
          return;
        }

        setUrl(config.url);
        setKey(config.key);
        setEnabled(config.enabled);
        setSelectedEvents(config.events);
        setEventGroups(events);
      })
      .catch((requestError) => {
        if (!cancelled) {
          setError(getErrorMessage(requestError, t('webhooks.loadError')));
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
  }, [opened, t]);

  function handleLanguageSelect(value: string | null) {
    if (value === 'es' || value === 'en') {
      void i18n.changeLanguage(value);
    }
  }

  function toggleEvent(event: string, checked: boolean) {
    setSelectedEvents((current) =>
      checked ? [...current, event] : current.filter((item) => item !== event)
    );
  }

  async function handleSave() {
    setSaving(true);
    setError(null);

    try {
      await webhooksApi.update({
        url: url.trim(),
        key: key.trim(),
        events: selectedEvents,
        enabled,
      });
      notifications.show({ message: t('webhooks.saved'), color: 'green' });
    } catch (requestError) {
      setError(getErrorMessage(requestError, t('webhooks.saveError')));
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal opened={opened} onClose={onClose} title={t('settings.title')} centered size="lg">
      <Tabs defaultValue="general">
        <Tabs.List>
          <Tabs.Tab value="general">{t('settings.general')}</Tabs.Tab>
          <Tabs.Tab value="webhooks">{t('webhooks.title')}</Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="general" pt="md">
          <Select
            label={t('common.language')}
            value={i18n.language === 'es' ? 'es' : 'en'}
            onChange={handleLanguageSelect}
            allowDeselect={false}
            data={[
              { value: 'es', label: t('settings.languageSpanish') },
              { value: 'en', label: t('settings.languageEnglish') },
            ]}
          />
        </Tabs.Panel>

        <Tabs.Panel value="webhooks" pt="md">
          {loading ? (
            <Group justify="center" py="md">
              <Loader size="sm" />
            </Group>
          ) : (
            <Stack gap="md">
              {error ? <Alert color="red">{error}</Alert> : null}

              <Switch
                label={t('webhooks.enabled')}
                checked={enabled}
                onChange={(event) => setEnabled(event.currentTarget.checked)}
              />

              <Group grow align="flex-end" gap="md">
                <TextInput
                  label={t('webhooks.url')}
                  placeholder={t('webhooks.urlPlaceholder')}
                  value={url}
                  onChange={(event) => setUrl(event.currentTarget.value)}
                />

                <TextInput
                  label={t('webhooks.key')}
                  placeholder={t('webhooks.keyPlaceholder')}
                  value={key}
                  onChange={(event) => setKey(event.currentTarget.value)}
                />
              </Group>

              <Stack gap="md">
                <Text fw={600} size="sm">
                  {t('webhooks.events')}
                </Text>

                {Object.entries(eventGroups).map(([group, events]) => (
                  <Stack key={group} gap="xs">
                    <Text fw={600} size="sm" c="dimmed">
                      {t(`webhooks.groups.${group}`, { defaultValue: groupLabel(group) })}
                    </Text>

                    {events.map((event) => (
                      <Switch
                        key={event}
                        size="sm"
                        label={event}
                        checked={selectedEvents.includes(event)}
                        onChange={(changeEvent) =>
                          toggleEvent(event, changeEvent.currentTarget.checked)
                        }
                      />
                    ))}
                  </Stack>
                ))}
              </Stack>

              <Group justify="flex-end">
                <Button variant="default" onClick={onClose}>
                  {t('common.cancel')}
                </Button>

                <Button onClick={() => void handleSave()} loading={saving}>
                  {t('webhooks.save')}
                </Button>
              </Group>
            </Stack>
          )}
        </Tabs.Panel>
      </Tabs>
    </Modal>
  );
}
