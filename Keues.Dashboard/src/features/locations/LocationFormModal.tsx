import {
  Button,
  ColorSwatch,
  Group,
  Modal,
  Stack,
  Text,
  TextInput,
  Textarea,
} from "@mantine/core";
import { IconCheck } from "@tabler/icons-react";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { colors } from "../../data/common";
import { LocationInput, LocationKeue} from "../../api/interfaces/Location/Locations";

interface LocationFormModalProps {
  opened: boolean;
  loading: boolean;
  initialLocation?: LocationKeue;
  onClose: () => void;
  onSubmit: (payload: LocationInput) => Promise<void>;
}

interface FormState {
  name: string;
  description: string;
  color: string;
}

function getInitialState(initialLocation?: LocationKeue): FormState {
  return {
    name: initialLocation?.name ?? "",
    description: initialLocation?.description ?? "",
    color: initialLocation?.color ?? "blue",
  };
}

export function LocationFormModal({
  opened,
  loading,
  initialLocation,
  onClose,
  onSubmit,
}: LocationFormModalProps) {
  const { t } = useTranslation();

  const [formState, setFormState] = useState<FormState>(
    getInitialState(initialLocation)
  );

  const [nameError, setNameError] = useState<string | null>(null);

  useEffect(() => {
    if (!opened) {
      return;
    }

    setFormState(getInitialState(initialLocation));
    setNameError(null);
  }, [opened, initialLocation]);

  const isEditing = Boolean(initialLocation);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const trimmedName = formState.name.trim();

    if (!trimmedName) {
      setNameError(t("locationForm.nameRequired"));
      return;
    }

    setNameError(null);

    await onSubmit({
      name: trimmedName,
      description: formState.description.trim(),
      color: formState.color,
    });
  }

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={
        isEditing
          ? t("locationForm.editTitle")
          : t("locationForm.createTitle")
      }
      centered
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <TextInput
            label={t("locationForm.name")}
            placeholder={t("locationForm.namePlaceholder")}
            value={formState.name}
            error={nameError}
            withAsterisk
            onChange={(event) => {
              const value = event.currentTarget.value;

              setFormState((previous) => ({
                ...previous,
                name: value,
              }));
            }}
          />

          <Textarea
            label={t("locationForm.description")}
            placeholder={t("locationForm.descriptionPlaceholder")}
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
            <Text fw={500}>{t("locationForm.color")}</Text>

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

          <Group justify="flex-end">
            <Button
              variant="default"
              onClick={onClose}
              disabled={loading}
            >
              {t("common.cancel")}
            </Button>

            <Button type="submit" loading={loading}>
              {isEditing
                ? t("locationForm.editAction")
                : t("locationForm.createAction")}
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}