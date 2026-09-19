import { Queue, QueueInput } from '@/api/interfaces/Queue/Queues';
import {
  Button,
  Card,
  ColorSwatch,
  Group,
  Modal,
  NumberInput,
  SimpleGrid,
  Stack,
  Switch,
  Tabs,
  Text,
  TextInput,
  Textarea,
  ThemeIcon,
  Tooltip,
} from '@mantine/core';
import { IconCheck, IconDeviceDesktop, IconDeviceTv, IconInfoCircle } from '@tabler/icons-react';
import { colors } from '../../data/common';

import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { countersApi } from '@/api/CountersApi';

interface QueueFormModalProps {
  opened: boolean;
  loading: boolean;
  initialQueue?: Queue;
  locationId: string;
  onClose: () => void;
  onSubmit: (payload: QueueInput) => Promise<void>;
}

interface QueueFormState {
  name: string;
  description: string;
  code: string;
  maxValue: string;
  priority: string;
  weight: string;
  agingIntervalMinutes: string;
  maxAgingBonus: string;
  color: string;
  resetAt: string;
  locaitonId: string;
}

interface CounterOption {
  id: string;
  code: string;
  name: string;
}

function padTimePart(value: number): string {
  return value.toString().padStart(2, '0');
}

function utcTimeToLocal(value?: string | null): string {
  if (!value) {
    return '00:00';
  }

  const [hours, minutes] = value.slice(0, 5).split(':').map(Number);

  const date = new Date();
  date.setUTCHours(hours, minutes, 0, 0);

  return `${padTimePart(date.getHours())}:${padTimePart(date.getMinutes())}`;
}

function localTimeToUtc(value: string): string {
  const [hours, minutes] = value.split(':').map(Number);

  const date = new Date();
  date.setHours(hours, minutes, 0, 0);

  return `${padTimePart(date.getUTCHours())}:${padTimePart(date.getUTCMinutes())}`;
}

function getInitialState(initialQueue?: Queue): QueueFormState {
  return {
    name: initialQueue?.name ?? '',
    description: initialQueue?.description ?? '',
    code: initialQueue?.code ?? '',
    maxValue: initialQueue?.maxValue?.toString() ?? '999',
    priority: initialQueue?.priority?.toString() ?? '0',
    weight: initialQueue?.weight?.toString() ?? '0',
    agingIntervalMinutes: initialQueue?.agingIntervalMinutes?.toString() ?? '0',
    maxAgingBonus: initialQueue?.maxAgingBonus?.toString() ?? '0',
    color: initialQueue?.color ?? 'blue',
    resetAt: utcTimeToLocal(initialQueue?.resetAt),
    locaitonId: initialQueue?.locationId ?? '',
  };
}

export function QueueFormModal(props: QueueFormModalProps) {
  const { t } = useTranslation();

  const { opened, loading, initialQueue, locationId, onClose, onSubmit } = props;

  const [formState, setFormState] = useState<QueueFormState>(getInitialState(initialQueue));

  const [nameError, setNameError] = useState<string | null>(null);
  const [maxValueError, setMaxValueError] = useState<string | null>(null);

  const [resetAtEnabled, setResetAtEnabled] = useState(Boolean(initialQueue?.resetAt));

  const [counters, setCounters] = useState<CounterOption[]>([]);
  const [selectedCounters, setSelectedCounters] = useState<string[]>([]);

  useEffect(() => {
    if (!opened) {
      return;
    }

    async function loadCounters() {
      try {
        const result = await countersApi.list(locationId);
        setCounters(result.data.sort((a, b) => a.name.localeCompare(b.name)));
      } catch {
        // Ignoramos errores al cargar counters.
      }
    }

    loadCounters();
  }, [opened, locationId]);

  useEffect(() => {
    if (!opened) {
      return;
    }

    setFormState(getInitialState(initialQueue));

    setNameError(null);
    setMaxValueError(null);

    setResetAtEnabled(Boolean(initialQueue?.resetAt));

    setSelectedCounters(initialQueue?.counters ?? []);
  }, [opened, initialQueue]);

  function toggleCounter(counterId: string) {
    setSelectedCounters((current) =>
      current.includes(counterId)
        ? current.filter((id) => id !== counterId)
        : [...current, counterId]
    );
  }

  const isEditing = Boolean(initialQueue);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const normalizedName = formState.name.trim();
    const normalizedDisplayCode = formState.code.trim().toUpperCase();
    const normalizedMaxValue = formState.maxValue.trim();

    if (!normalizedName) {
      setNameError(t('ticketTypeForm.nameRequired'));
      return;
    }

    let parsedMaxValue: number | null = null;

    if (normalizedMaxValue.length > 0) {
      const value = Number.parseInt(normalizedMaxValue, 10);

      if (!Number.isInteger(value) || value < 0) {
        setMaxValueError(t('ticketTypeForm.maxValueInvalid'));
        return;
      }

      parsedMaxValue = value === 0 ? null : value;
    }

    await onSubmit({
      name: normalizedName,
      description: formState.description.trim(),
      code: normalizedDisplayCode,
      maxValue: parsedMaxValue,
      priority: Number.parseInt(formState.priority, 10) || 0,
      weight: Number.parseInt(formState.weight, 10) || 0,
      agingIntervalMinutes: Number.parseInt(formState.agingIntervalMinutes, 10) || 0,
      maxAgingBonus: Number.parseInt(formState.maxAgingBonus, 10) || 0,
      color: formState.color,
      locationId: locationId,
      counters: selectedCounters,
      resetAt: resetAtEnabled ? localTimeToUtc(formState.resetAt || '00:00') : null,
    });
  }
  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={isEditing ? t('ticketTypeForm.editTitle') : t('ticketTypeForm.createTitle')}
      centered
      size="lg"
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <Tabs defaultValue="general" keepMounted={false}>
            <Tabs.List>
              <Tabs.Tab value="general">{t('ticketTypeForm.tabGeneral')}</Tabs.Tab>
              <Tabs.Tab value="rules">{t('ticketTypeForm.tabRules')}</Tabs.Tab>
              <Tabs.Tab value="counters">{t('ticketTypeForm.tabCounters')}</Tabs.Tab>
            </Tabs.List>

            <Tabs.Panel
              value="general"
              pt="md"
              h="max(280px, min(460px, calc(100dvh - 260px)))"
              style={{ overflowY: 'auto' }}
            >
              <Stack gap="md">
                <Group align="flex-start" wrap="nowrap">
                  <Stack gap={4} style={{ flex: 1 }}>
                    <Text fw={600}>{t('ticketTypeForm.name')}</Text>

                    <TextInput
                      required
                      value={formState.name}
                      onChange={(event) => {
                        const value = event.currentTarget.value;

                        setFormState((previous) => ({
                          ...previous,
                          name: value,
                        }));
                      }}
                      error={nameError}
                    />
                  </Stack>

                  <Stack gap={4} style={{ width: 80, flexShrink: 0 }}>
                    <Text fw={600}>{t('queueForm.prefix')}</Text>

                    <TextInput
                      maxLength={2}
                      leftSection={<IconDeviceTv size={16} />}
                      value={formState.code}
                      onChange={(event) => {
                        const value = event.currentTarget.value
                          .toUpperCase()
                          .replace(/[^A-Z]/g, '')
                          .slice(0, 2);

                        setFormState((previous) => ({
                          ...previous,
                          code: value,
                        }));
                      }}
                    />
                  </Stack>
                </Group>

                <Stack gap="xs">
                  <Text fw={600}>{t('queueForm.color')}</Text>

                  <SimpleGrid cols={6} spacing="sm">
                    {colors.map((color) => (
                      <ColorSwatch
                        key={color}
                        color={`var(--mantine-color-${color}-6)`}
                        style={{
                          cursor: 'pointer',
                          borderRadius: '50%',
                          border:
                            formState.color === color
                              ? '2px solid var(--mantine-color-black)'
                              : '2px solid transparent',
                        }}
                        onClick={() =>
                          setFormState((previous) => ({
                            ...previous,
                            color,
                          }))
                        }
                      >
                        {formState.color === color && <IconCheck size={14} color="white" />}
                      </ColorSwatch>
                    ))}
                  </SimpleGrid>
                </Stack>

                <Stack gap={4}>
                  <Text fw={600}>{t('ticketTypeForm.description')}</Text>

                  <Textarea
                    value={formState.description}
                    onChange={(event) => {
                      const value = event.currentTarget.value;

                      setFormState((previous) => ({
                        ...previous,
                        description: value,
                      }));
                    }}
                  />
                </Stack>
              </Stack>
            </Tabs.Panel>

            <Tabs.Panel
              value="rules"
              pt="md"
              h="max(280px, min(460px, calc(100dvh - 260px)))"
              style={{ overflowY: 'auto' }}
            >
              <Stack gap="md">
                <SimpleGrid cols={{ base: 1, md: 3 }}>
                  <Stack gap={4}>
                    <Text fw={600}>
                      <Group gap={6} wrap="nowrap">
                        <span>{t('queueForm.maxValue')}</span>
                        <Tooltip label={t('queueForm.maxValueHelp')} withArrow>
                          <IconInfoCircle size={14} style={{ cursor: 'pointer', flexShrink: 0 }} />
                        </Tooltip>
                      </Group>
                    </Text>

                    <NumberInput
                      value={formState.maxValue === '' ? undefined : Number(formState.maxValue)}
                      onChange={(value) =>
                        setFormState((previous) => ({
                          ...previous,
                          maxValue: value == null ? '' : String(value),
                        }))
                      }
                      min={0}
                      error={maxValueError}
                    />
                  </Stack>

                  <Stack gap={4}>
                    <Text fw={600}>{t('queueForm.priority')}</Text>

                    <NumberInput
                      value={Number(formState.priority)}
                      onChange={(value) =>
                        setFormState((previous) => ({
                          ...previous,
                          priority: String(value ?? 0),
                        }))
                      }
                    />
                  </Stack>

                  <Stack gap={4}>
                    <Text fw={600}>
                      <Group gap={6} wrap="nowrap">
                        <span>{t('queueForm.weight')}</span>
                        <Tooltip label={t('queueForm.weightHelp')} withArrow>
                          <IconInfoCircle size={14} style={{ cursor: 'pointer', flexShrink: 0 }} />
                        </Tooltip>
                      </Group>
                    </Text>

                    <NumberInput
                      value={Number(formState.weight)}
                      onChange={(value) =>
                        setFormState((previous) => ({
                          ...previous,
                          weight: String(value ?? 0),
                        }))
                      }
                    />
                  </Stack>
                </SimpleGrid>

                <SimpleGrid cols={{ base: 1, sm: 2 }}>
                  <Stack gap={4}>
                    <Text fw={600}>
                      <Group gap={6} wrap="nowrap">
                        <span>{t('queueForm.agingIntervalMinutes')}</span>
                        <Tooltip label={t('queueForm.agingIntervalHelp')} withArrow>
                          <IconInfoCircle size={14} style={{ cursor: 'pointer', flexShrink: 0 }} />
                        </Tooltip>
                      </Group>
                    </Text>

                    <NumberInput
                      value={Number(formState.agingIntervalMinutes)}
                      onChange={(value) =>
                        setFormState((previous) => ({
                          ...previous,
                          agingIntervalMinutes: String(value ?? 0),
                        }))
                      }
                    />
                  </Stack>

                  <Stack gap={4}>
                    <Text fw={600}>
                      <Group gap={6} wrap="nowrap">
                        <span>{t('queueForm.maxAgingBonus')}</span>
                        <Tooltip label={t('queueForm.maxAgingBonusHelp')} withArrow>
                          <IconInfoCircle size={14} style={{ cursor: 'pointer', flexShrink: 0 }} />
                        </Tooltip>
                      </Group>
                    </Text>

                    <NumberInput
                      value={Number(formState.maxAgingBonus)}
                      onChange={(value) =>
                        setFormState((previous) => ({
                          ...previous,
                          maxAgingBonus: String(value ?? 0),
                        }))
                      }
                    />
                  </Stack>
                </SimpleGrid>

                <Stack gap="xs">
                  <Group gap={6} wrap="nowrap">
                    <Switch
                      checked={resetAtEnabled}
                      label={t('queueForm.resetAt')}
                      onChange={(event) => {
                        const checked = event.currentTarget.checked;

                        setResetAtEnabled(checked);

                        if (checked && !formState.resetAt) {
                          setFormState((previous) => ({
                            ...previous,
                            resetAt: '00:00',
                          }));
                        }
                      }}
                    />

                    <Tooltip label={t('queueForm.resetAtHelp')} withArrow>
                      <IconInfoCircle size={14} style={{ cursor: 'pointer', flexShrink: 0 }} />
                    </Tooltip>
                  </Group>

                  {resetAtEnabled && (
                    <TextInput
                      type="time"
                      value={formState.resetAt}
                      onChange={(event) => {
                        const value = event.currentTarget.value;

                        setFormState((previous) => ({
                          ...previous,
                          resetAt: value,
                        }));
                      }}
                      style={{ maxWidth: 160 }}
                    />
                  )}
                </Stack>
              </Stack>
            </Tabs.Panel>

            <Tabs.Panel
              value="counters"
              pt="md"
              h="max(280px, min(460px, calc(100dvh - 260px)))"
              style={{ overflowY: 'auto' }}
            >
              <Stack gap="sm">
                <Text size="xs" c="dimmed">
                  {t('queueForm.noCountersMeansAll')}
                </Text>

                <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
                  {counters.map((counter) => {
                    const enabled = selectedCounters.includes(counter.id);

                    return (
                      <Card
                        key={counter.id}
                        withBorder
                        padding="sm"
                        radius="md"
                        style={{
                          cursor: 'pointer',
                          borderColor: enabled ? 'var(--mantine-color-blue-5)' : undefined,
                          backgroundColor: enabled ? 'var(--mantine-color-blue-0)' : undefined,
                        }}
                        onClick={() => toggleCounter(counter.id)}
                      >
                        <Group justify="space-between" wrap="nowrap">
                          <Group gap="sm">
                            <ThemeIcon
                              size={34}
                              radius="md"
                              variant={enabled ? 'light' : 'default'}
                              color={enabled ? 'blue' : 'gray'}
                            >
                              <IconDeviceDesktop size={18} />
                            </ThemeIcon>

                            <div>
                              <Text size="sm" fw={600}>
                                {counter.name}
                              </Text>

                              <Text size="xs" c="dimmed">
                                {counter.code}
                              </Text>
                            </div>
                          </Group>

                          <div onClick={(event) => event.stopPropagation()}>
                            <Switch checked={enabled} onChange={() => toggleCounter(counter.id)} />
                          </div>
                        </Group>
                      </Card>
                    );
                  })}
                </SimpleGrid>
              </Stack>
            </Tabs.Panel>
          </Tabs>

          <Group justify="flex-end">
            <Button variant="default" onClick={onClose} disabled={loading}>
              {t('common.cancel')}
            </Button>

            <Button type="submit" loading={loading}>
              {isEditing ? t('ticketTypeForm.editAction') : t('ticketTypeForm.createAction')}
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
