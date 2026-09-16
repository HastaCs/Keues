import { Alert, Button, ColorSwatch, Group, Modal, Stack, Text, TextInput } from '@mantine/core';
import { IconCheck } from '@tabler/icons-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { CreateUserGroupInput, UserGroup } from '@/api/interfaces/UserGroup/UserGroups';
import { colors } from '@/data/common';

interface UserGroupFormModalProps {
  opened: boolean;
  loading: boolean;
  error: string | null;
  initialGroup?: UserGroup;
  locationId: string;
  onClose: () => void;
  onSubmit: (payload: CreateUserGroupInput) => Promise<void>;
}

interface FormState {
  name: string;
  color: string;
}

function getInitialState(initialGroup?: UserGroup): FormState {
  return {
    name: initialGroup?.name ?? '',
    color: initialGroup?.color ?? 'blue',
  };
}

export function UserGroupFormModal({
  opened,
  loading,
  error,
  initialGroup,
  locationId,
  onClose,
  onSubmit,
}: UserGroupFormModalProps) {
  const { t } = useTranslation();

  const [formState, setFormState] = useState<FormState>(getInitialState(initialGroup));
  const [nameError, setNameError] = useState<string | null>(null);

  useEffect(() => {
    if (!opened) {
      return;
    }

    setFormState(getInitialState(initialGroup));
    setNameError(null);
  }, [opened, initialGroup]);

  const isEditing = Boolean(initialGroup);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const name = formState.name.trim();

    if (name.length === 0) {
      setNameError(t('userGroupForm.nameRequired'));
      return;
    }

    setNameError(null);

    await onSubmit({
      name,
      color: formState.color,
      locationId,
    });
  }

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={isEditing ? t('userGroupForm.editTitle') : t('userGroupForm.createTitle')}
      centered
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          {error ? <Alert color="red">{error}</Alert> : null}

          <TextInput
            label={t('userGroupForm.name')}
            placeholder={t('userGroupForm.namePlaceholder')}
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

          <Stack gap="xs">
            <Text fw={500}>{t('userGroupForm.color')}</Text>

            <Group gap="sm">
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
            </Group>
          </Stack>

          <Group justify="flex-end">
            <Button variant="default" onClick={onClose} disabled={loading}>
              {t('common.cancel')}
            </Button>

            <Button type="submit" loading={loading}>
              {isEditing ? t('userGroupForm.editAction') : t('userGroupForm.createAction')}
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
