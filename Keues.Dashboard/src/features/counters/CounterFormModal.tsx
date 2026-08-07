import {
  Button,
  Card,
  ColorSwatch,
  Group,
  Modal,
  SimpleGrid,
  Stack,
  Switch,
  Text,
  Textarea,
  TextInput,
  ThemeIcon,
} from "@mantine/core";

import { IconCheck, IconTicket } from "@tabler/icons-react";
import {colors} from "../../data/common";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { queuesApi } from "@/api/QueuesApi";
import type { Counter, CreateCounterInput } from "@/api/interfaces/Counter/Counters";
import { Queue } from "@/api/interfaces/Queue/Queues";

interface CounterFormModalProps {
  opened: boolean;
  loading: boolean;
  initialCounter?: Counter;
  locationId: string;
  onClose: () => void;
  onSubmit: (payload: CreateCounterInput) => Promise<void>;
}

interface CounterFormState {
  code: string;
  name: string;
  description: string;
  color:string
}

function getInitialState(initialCounter?: Counter): CounterFormState {
  return {
    code: initialCounter?.code ?? "",
    name: initialCounter?.name ?? "",
    description: initialCounter?.description ?? "",
    color: initialCounter?.color ?? "blue",
  };
}

export function CounterFormModal(props: CounterFormModalProps) {
  const { t } = useTranslation();

  const { opened, loading, initialCounter, locationId, onClose, onSubmit } = props;

  const [formState, setFormState] = useState<CounterFormState>(getInitialState(initialCounter));

  const [queues, setQueues] = useState<Queue[]>([]);

  const [selectedQueues, setSelectedQueues] = useState<string[]>([]);

  const [codeError, setCodeError] = useState<string | null>(null);

  const [nameError, setNameError] = useState<string | null>(null);

  useEffect(() => {
    if (!opened) {
      return;
    }

    async function loadQueues() {
      try {
        const types = await queuesApi.list(locationId);

        setQueues(types.data);
      } catch {
        // Ignoramos errores al cargar tipos de ticket.
      }
    }

    loadQueues();
  }, [opened, locationId]);

  useEffect(() => {
    if (!opened) {
      return;
    }

    setFormState(getInitialState(initialCounter));

   setSelectedQueues(initialCounter?.queues?? []);

    setCodeError(null);
    setNameError(null);
  }, [opened, initialCounter]);

  function toggleQueue(ticketTypeId: string) {
   
    setSelectedQueues((current) => {
      if (current.includes(ticketTypeId)) {
        return current.filter((id) => id !== ticketTypeId);
      }

      return [...current, ticketTypeId];
    });
  }

  const isEditing = Boolean(initialCounter);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const normalizedCode = formState.code.trim().toUpperCase();

    const normalizedName = formState.name.trim();

    if (normalizedCode.length === 0) {
      setCodeError(t("counterForm.codeRequired"));

      return;
    }

    if (normalizedName.length === 0) {
      setNameError(t("counterForm.nameRequired"));

      return;
    }

    setCodeError(null);
    setNameError(null);

    await onSubmit({
      code: normalizedCode,
      name: normalizedName,
      description: formState.description.trim(),
      color: formState.color,
      locationId,
      queues: selectedQueues,
    } );
  }

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={isEditing ? t("counterForm.editTitle") : t("counterForm.createTitle")}
      centered
      size="lg"
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <TextInput
            label={t("counterForm.code")}
            value={formState.code}
            onChange={(event) => {
              const value = event.currentTarget.value;

              setFormState((previous) => ({
                ...previous,
                code: value,
              }));
            }}
            error={codeError}
            withAsterisk
          />

          <TextInput
            label={t("counterForm.name")}
            value={formState.name}
            onChange={(event) => {
              const value = event.currentTarget.value;

              setFormState((previous) => ({
                ...previous,
                name: value,
              }));
            }}
            error={nameError}
            withAsterisk
          />

          <Textarea
            label={t("counterForm.description")}
            minRows={3}
            value={formState.description}
            onChange={(event) => {
              const value = event.currentTarget.value;

              setFormState((previous) => ({
                ...previous,
                description: value,
              }));
            }}
          />
 <Stack gap="xs">
            <Text fw={500}>{t("counterForm.color")}</Text>

            <Group gap="sm">
              {colors.map((color) => (
                <ColorSwatch
                  key={color}
                  color={`var(--mantine-color-${color}-6)`}
                  style={{
                    cursor: "pointer",
                    borderRadius: "50%",
                    border:
                      formState.color === color
                        ? "2px solid var(--mantine-color-black)"
                        : "2px solid transparent",
                  }}
                  onClick={() =>
                    setFormState((previous) => ({
                      ...previous,
                      color,
                    }))
                  }
                >
                  {formState.color === color && (
                    <IconCheck size={14} color="white" />
                  )}
                </ColorSwatch>
              ))}
            </Group>
          </Stack>
          <Stack gap="sm">
            <Text fw={600}>{t("counterForm.allowedTicketTypes")}</Text>

            <SimpleGrid
              cols={{
                base: 1,
                sm: 2,
              }}
              spacing="sm"
            >
              {queues.map((ticket) => {
                const enabled = selectedQueues.includes(ticket.id);

             return (
  <Card
    key={ticket.id}
    withBorder
    padding="sm"
    radius="md"
    style={{
      cursor: "pointer",
      borderColor: enabled
        ? "var(--mantine-color-blue-5)"
        : undefined,
      backgroundColor: enabled
        ? "var(--mantine-color-blue-0)"
        : undefined,
    }}
    onClick={() => toggleQueue(ticket.id)}
  >
    <Group justify="space-between" wrap="nowrap">
      <Group gap="sm">
        <ThemeIcon
          size={34}
          radius="md"
          variant={enabled ? "light" : "default"}
          color={enabled ? "blue" : "gray"}
        >
          <IconTicket size={18} />
        </ThemeIcon>

        <div>
          <Text size="sm" fw={600}>
            {ticket.code}
          </Text>

          <Text size="xs" c="dimmed">
            {ticket.name}
          </Text>
        </div>
      </Group>

      <div
        onClick={(event) => event.stopPropagation()}
      >
        <Switch
          checked={enabled}
          onChange={() => toggleQueue(ticket.id)}
        />
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
              {isEditing ? t("counterForm.editAction") : t("counterForm.createAction")}
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
