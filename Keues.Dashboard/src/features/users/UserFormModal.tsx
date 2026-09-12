import { Alert, Button, Group, Modal, PasswordInput, Stack, Text, TextInput } from '@mantine/core';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { CreateUserInput, User } from '@/api/interfaces/User/Users';

interface UserFormModalProps {
  opened: boolean;
  loading: boolean;
  error: string | null;
  initialUser?: User;
  locationId: string;
  onClose: () => void;
  onSubmit: (payload: CreateUserInput) => Promise<void>;
}

interface FormState {
  name: string;
  email: string;
  password: string;
  confirmPassword: string;
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function getInitialState(initialUser?: User): FormState {
  return {
    name: initialUser?.name ?? '',
    email: initialUser?.email ?? '',
    password: '',
    confirmPassword: '',
  };
}

export function UserFormModal({
  opened,
  loading,
  error,
  initialUser,
  locationId,
  onClose,
  onSubmit,
}: UserFormModalProps) {
  const { t } = useTranslation();

  const [formState, setFormState] = useState<FormState>(getInitialState(initialUser));

  const [nameError, setNameError] = useState<string | null>(null);
  const [emailError, setEmailError] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);

  useEffect(() => {
    if (!opened) {
      return;
    }

    setFormState(getInitialState(initialUser));
    setNameError(null);
    setEmailError(null);
    setPasswordError(null);
  }, [opened, initialUser]);

  const isEditing = Boolean(initialUser);
  const showConfirmPassword = !isEditing || formState.password.length > 0;

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const name = formState.name.trim();
    const email = formState.email.trim();
    const password = formState.password;

    let valid = true;

    if (name.length === 0) {
      setNameError(t('userForm.nameRequired'));
      valid = false;
    } else {
      setNameError(null);
    }

    if (email.length === 0) {
      setEmailError(t('userForm.emailRequired'));
      valid = false;
    } else if (!EMAIL_PATTERN.test(email)) {
      setEmailError(t('userForm.emailInvalid'));
      valid = false;
    } else {
      setEmailError(null);
    }

    if (!isEditing && password.length === 0) {
      setPasswordError(t('userForm.passwordRequired'));
      valid = false;
    } else if (password.length > 0 && password !== formState.confirmPassword) {
      setPasswordError(t('userForm.passwordsMismatch'));
      valid = false;
    } else {
      setPasswordError(null);
    }

    if (!valid) {
      return;
    }

    await onSubmit({
      name,
      email,
      password,
      locationId,
    });
  }

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={isEditing ? t('userForm.editTitle') : t('userForm.createTitle')}
      centered
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          {error ? <Alert color="red">{error}</Alert> : null}

          <TextInput
            label={t('userForm.name')}
            placeholder={t('userForm.namePlaceholder')}
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

          <TextInput
            label={t('userForm.email')}
            placeholder={t('userForm.emailPlaceholder')}
            type="email"
            value={formState.email}
            error={emailError}
            withAsterisk
            onChange={(event) => {
              const value = event.currentTarget.value;

              setFormState((previous) => ({
                ...previous,
                email: value,
              }));
            }}
          />

          <PasswordInput
            label={t('userForm.password')}
            placeholder={t('userForm.passwordPlaceholder')}
            value={formState.password}
            error={passwordError}
            withAsterisk={!isEditing}
            onChange={(event) => {
              const value = event.currentTarget.value;

              setFormState((previous) => ({
                ...previous,
                password: value,
              }));
            }}
          />

          {isEditing ? (
            <Text size="xs" c="dimmed">
              {t('userForm.passwordHelp')}
            </Text>
          ) : null}

          {showConfirmPassword ? (
            <PasswordInput
              label={t('userForm.confirmPassword')}
              placeholder={t('userForm.passwordPlaceholder')}
              value={formState.confirmPassword}
              withAsterisk={!isEditing}
              onChange={(event) => {
                const value = event.currentTarget.value;

                setFormState((previous) => ({
                  ...previous,
                  confirmPassword: value,
                }));
              }}
            />
          ) : null}

          <Group justify="flex-end">
            <Button variant="default" onClick={onClose} disabled={loading}>
              {t('common.cancel')}
            </Button>

            <Button type="submit" loading={loading}>
              {isEditing ? t('userForm.editAction') : t('userForm.createAction')}
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
