import { Queue, QueueInput } from "@/api/interfaces/Queue/Queues";
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
  Text,
  TextInput,
  Textarea,
  ThemeIcon,
} from "@mantine/core";
import { IconCheck, IconDeviceDesktop } from "@tabler/icons-react";
import { colors } from "../../data/common";

import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";

import { countersApi } from "@/api/CountersApi";

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
  locaitonId: string;
}

interface CounterOption {
  id: string;
  code: string;
  name: string;
}

function getInitialState(initialQueue?: Queue): QueueFormState {
  return {
    name: initialQueue?.name ?? "",
    description: initialQueue?.description ?? "",
    code: initialQueue?.code ?? "",
    maxValue: initialQueue?.maxValue?.toString() ?? "",
    priority: initialQueue?.priority?.toString() ?? "0",
    weight: initialQueue?.weight?.toString() ?? "0",
    agingIntervalMinutes: initialQueue?.agingIntervalMinutes?.toString() ?? "0",
    maxAgingBonus: initialQueue?.maxAgingBonus?.toString() ?? "0",
    color: initialQueue?.color ?? "blue",
    locaitonId: initialQueue?.locationId ?? "",
  };
}

export function QueueFormModal(props: QueueFormModalProps) {


  const { t } = useTranslation();

  const { opened, loading, initialQueue, locationId, onClose, onSubmit } = props;


  const [formState, setFormState] = useState<QueueFormState>(getInitialState(initialQueue));

  const [nameError, setNameError] = useState<string | null>(null);
  const [maxValueError, setMaxValueError] = useState<string | null>(null);

  const [counters, setCounters] = useState<CounterOption[]>([]);
  const [selectedCounters, setSelectedCounters] = useState<string[]>([]);

  useEffect(() => {
    if (!opened) {
      return;
    }

    async function loadCounters() {
      try {
        const result = await countersApi.list(locationId);
        setCounters(result.data);
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

    setSelectedCounters(initialQueue?.counters ?? []);
  }, [opened, initialQueue]);

  function toggleCounter(counterId: string) {
    setSelectedCounters((current) =>
      current.includes(counterId) ? current.filter((id) => id !== counterId) : [...current, counterId],
    );
  }

  const isEditing = Boolean(initialQueue);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const normalizedName = formState.name.trim();
    const normalizedDisplayCode = formState.code.trim().toUpperCase();
    const normalizedMaxValue = formState.maxValue.trim();

    if (!normalizedName) {
      setNameError(t("ticketTypeForm.nameRequired"));
      return;
    }

    let parsedMaxValue: number | null = null;

    if (normalizedMaxValue.length > 0) {
      const value = Number.parseInt(normalizedMaxValue, 10);

      if (!Number.isInteger(value) || value < 1) {
        setMaxValueError(t("ticketTypeForm.maxValueInvalid"));
        return;
      }

      parsedMaxValue = value;
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
    } );
  }
  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={isEditing ? t("ticketTypeForm.editTitle") : t("ticketTypeForm.createTitle")}
      centered
      size="lg"
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <Group align="flex-start" wrap="nowrap">
            <TextInput
              style={{ flex: 1 }}
              label={t("ticketTypeForm.name")}
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

           <TextInput
  style={{ width: 80, flexShrink: 0 }}
  maxLength={2}
  label={t("queueForm.prefix")}
  value={formState.code}
  onChange={(event) => {
    const value = event.currentTarget.value
      .toUpperCase()
      .replace(/[^A-Z]/g, "")
      .slice(0, 2);

    setFormState((previous) => ({
      ...previous,
      code: value,
    }));
  }}
/>
          </Group>

          <SimpleGrid
            cols={{
              base: 1,
              md: 3,
            }}
          >
            <NumberInput
              label={t("queueForm.maxValue")}
              value={formState.maxValue === "" ? undefined : Number(formState.maxValue)}
              onChange={(value) =>
                setFormState((previous) => ({
                  ...previous,
                  maxValue: value == null ? "" : String(value),
                }))
              }
              min={1}
              error={maxValueError}
            />

            <NumberInput
              label={t("queueForm.priority")}
              value={Number(formState.priority)}
              onChange={(value) =>
                setFormState((previous) => ({
                  ...previous,
                  priority: String(value ?? 0),
                }))
              }
            />

            <NumberInput
              label={t("queueForm.weight")}
              value={Number(formState.weight)}
              onChange={(value) =>
                setFormState((previous) => ({
                  ...previous,
                  weight: String(value ?? 0),
                }))
              }
            />
          </SimpleGrid>

          <NumberInput
            label={t("queueForm.agingIntervalMinutes")}
            value={Number(formState.agingIntervalMinutes)}
            onChange={(value) =>
              setFormState((previous) => ({
                ...previous,
                agingIntervalMinutes: String(value ?? 0),
              }))
            }
          />

          <NumberInput
            label={t("queueForm.maxAgingBonus")}
            value={Number(formState.maxAgingBonus)}
            onChange={(value) =>
              setFormState((previous) => ({
                ...previous,
                maxAgingBonus: String(value ?? 0),
              }))
            }
          />

          <Stack gap="xs">
            <Text fw={500}>{t("queueForm.color")}</Text>

            <Group gap="sm">
              {colors.map((color) => (
                <ColorSwatch
                  key={color}
                  color={`var(--mantine-color-${color}-6)`}
                  style={{
                    cursor: "pointer",
                    borderRadius: "50%",
                    border:
                      formState.color === color ? "2px solid var(--mantine-color-black)" : "2px solid transparent",
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
            </Group>
          </Stack>
<Textarea
  label={t("ticketTypeForm.description")}
  value={formState.description}
  onChange={(event) => {
    const value = event.currentTarget.value;

    setFormState((previous) => ({
      ...previous,
      description: value,
    }));
  }}
/>

          <Stack gap="sm">
            <Text fw={600}>{t("queueForm.allowedCounters")}</Text>

            <SimpleGrid
              cols={{
                base: 1,
                sm: 2,
              }}
              spacing="sm"
            >
              {counters.map((counter) => {
                const enabled = selectedCounters.includes(counter.id);

                return (
                  <Card
                    key={counter.id}
                    withBorder
                    padding="sm"
                    radius="md"
                    style={{
                      cursor: "pointer",
                      borderColor: enabled ? "var(--mantine-color-blue-5)" : undefined,
                      backgroundColor: enabled ? "var(--mantine-color-blue-0)" : undefined,
                    }}
                    onClick={() => toggleCounter(counter.id)}
                  >
                    <Group justify="space-between" wrap="nowrap">
                      <Group gap="sm">
                        <ThemeIcon
                          size={34}
                          radius="md"
                          variant={enabled ? "light" : "default"}
                          color={enabled ? "blue" : "gray"}
                        >
                          <IconDeviceDesktop size={18} />
                        </ThemeIcon>

                        <div>
                          <Text size="sm" fw={600}>
                            {counter.code}
                          </Text>

                          <Text size="xs" c="dimmed">
                            {counter.name}
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

          <Group justify="flex-end">
            <Button variant="default" onClick={onClose} disabled={loading}>
              {t("common.cancel")}
            </Button>

            <Button type="submit" loading={loading}>
              {isEditing ? t("ticketTypeForm.editAction") : t("ticketTypeForm.createAction")}
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
