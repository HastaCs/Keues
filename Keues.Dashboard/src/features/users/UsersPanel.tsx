import {
  ActionIcon,
  Alert,
  Avatar,
  Badge,
  Box,
  Button,
  Card,
  Divider,
  Group,
  Loader,
  Modal,
  Pagination,
  Paper,
  Select,
  SimpleGrid,
  Stack,
  Switch,
  Text,
  TextInput,
  Tooltip,
} from '@mantine/core';
import { IconMail, IconSearch, IconTrash } from '@tabler/icons-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { ApiError } from '@/api/httpClient';
import { usersApi } from '@/api/UsersApi';
import type { CreateUserInput, User } from '@/api/interfaces/User/Users';
import { PageHeader } from '@/components/PageHeader/PageHeader';
import { useActiveLocation } from '@/features/locations/LocationContext';
import styles from '@/styles/hover-card.module.css';
import { UserFormModal } from './UserFormModal';

type SortOrder = 'asc' | 'desc';

const PAGE_SIZE = 12;

const HIDE_INACTIVE_STORAGE_KEY = 'keues.users.hideInactive';

function getStoredHideInactive(): boolean {
  return window.localStorage.getItem(HIDE_INACTIVE_STORAGE_KEY) === 'true';
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

function formatDate(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '-';
  }

  return date.toLocaleDateString();
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

export function UsersPanel() {
  const { t } = useTranslation();
  const location = useActiveLocation();

  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [sortOrder, setSortOrder] = useState<SortOrder>('asc');
  const [hideInactive, setHideInactive] = useState(getStoredHideInactive);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE);
  const [pagination, setPagination] = useState<{ total: number; totalPages: number }>({
    total: 0,
    totalPages: 1,
  });
  const [reloadKey, setReloadKey] = useState(0);
  const [formOpened, setFormOpened] = useState(false);
  const [editingUser, setEditingUser] = useState<User | undefined>(undefined);
  const [deletingUser, setDeletingUser] = useState<User | undefined>(undefined);
  const [deleting, setDeleting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    const handle = window.setTimeout(() => {
      setDebouncedSearch(search.trim());
    }, 300);

    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, sortOrder, hideInactive, pageSize]);

  useEffect(() => {
    if (!location) {
      return;
    }

    let cancelled = false;

    setError(null);
    setLoading(true);

    usersApi
      .list({
        locationId: location.id,
        name: debouncedSearch || undefined,
        isActive: hideInactive ? true : undefined,
        page,
        limit: pageSize,
        sortOrder,
      })
      .then((response) => {
        if (cancelled) {
          return;
        }

        setUsers(response.data);
        setPagination({
          total: response.pagination?.total ?? 0,
          totalPages: response.pagination?.totalPages ?? 1,
        });
      })
      .catch((requestError) => {
        if (!cancelled) {
          setError(getErrorMessage(requestError, t('errors.unexpected')));
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
  }, [location, debouncedSearch, page, pageSize, sortOrder, hideInactive, reloadKey, t]);

  function openCreateModal() {
    setEditingUser(undefined);
    setFormError(null);
    setFormOpened(true);
  }

  async function openEditModal(user: User) {
    setError(null);

    try {
      const fullUser = await usersApi.get(user.id);
      setEditingUser(fullUser);
      setFormError(null);
      setFormOpened(true);
    } catch (requestError) {
      setError(getErrorMessage(requestError, t('errors.unexpected')));
    }
  }

  async function toggleEnabled(user: User, isEnabled: boolean) {
    setError(null);
    setUsers((current) =>
      current.map((item) => (item.id === user.id ? { ...item, enabled: isEnabled } : item))
    );

    try {
      await usersApi.setEnabled(user.id, isEnabled);
    } catch (requestError) {
      setUsers((current) =>
        current.map((item) => (item.id === user.id ? { ...item, enabled: !isEnabled } : item))
      );
      setError(getErrorMessage(requestError, t('errors.unexpected')));
    }
  }

  async function handleSubmitUser(payload: CreateUserInput) {
    setSaving(true);
    setFormError(null);

    try {
      if (editingUser) {
        await usersApi.update({ id: editingUser.id, ...payload });
      } else {
        await usersApi.create(payload);
      }

      setFormOpened(false);
      setEditingUser(undefined);
      setFormError(null);
      setReloadKey((current) => current + 1);
    } catch (requestError) {
      setFormError(getErrorMessage(requestError, t('errors.unexpected')));
    } finally {
      setSaving(false);
    }
  }

  async function handleConfirmDeleteUser() {
    if (!deletingUser) {
      return;
    }

    setDeleting(true);
    setError(null);

    try {
      await usersApi.remove(deletingUser.id);
      setDeletingUser(undefined);
      setReloadKey((current) => current + 1);
    } catch (requestError) {
      setError(getErrorMessage(requestError, t('errors.unexpected')));
    } finally {
      setDeleting(false);
    }
  }

  if (!location) {
    return null;
  }

  return (
    <>
      <Modal
        opened={Boolean(deletingUser)}
        onClose={() => setDeletingUser(undefined)}
        title={t('users.deleteTitle')}
        centered
      >
        <Stack gap="lg">
          <Text>{t('users.deleteDescription', { name: deletingUser?.name ?? '' })}</Text>

          <Group justify="flex-end">
            <Button
              variant="default"
              onClick={() => setDeletingUser(undefined)}
              disabled={deleting}
            >
              {t('common.cancel')}
            </Button>

            <Button
              color="red"
              leftSection={<IconTrash size={14} />}
              loading={deleting}
              onClick={handleConfirmDeleteUser}
            >
              {t('common.delete')}
            </Button>
          </Group>
        </Stack>
      </Modal>

      <UserFormModal
        opened={formOpened}
        loading={saving}
        error={formError}
        initialUser={editingUser}
        locationId={location.id}
        onClose={() => {
          setFormOpened(false);
          setFormError(null);
        }}
        onSubmit={handleSubmitUser}
      />

      <Stack gap="lg">
        <PageHeader
          label={t('users.title')}
          title={location.name}
          description={t('users.subtitle')}
          actions={<Button onClick={openCreateModal}>{t('users.newUser')}</Button>}
        />

        {error ? (
          <Alert color="red" title={t('errors.requestFailed')}>
            {error}
          </Alert>
        ) : null}

        <Group align="flex-end" justify="space-between" wrap="wrap">
          <TextInput
            value={search}
            onChange={(event) => setSearch(event.currentTarget.value)}
            placeholder={t('users.searchPlaceholder')}
            leftSection={<IconSearch size={16} />}
            style={{ maxWidth: 420, width: '100%' }}
          />

          <Group gap="lg" align="center">
            <Switch
              label={t('users.hideInactive')}
              checked={hideInactive}
              onChange={(event) => {
                const checked = event.currentTarget.checked;
                setHideInactive(checked);
                window.localStorage.setItem(HIDE_INACTIVE_STORAGE_KEY, String(checked));
              }}
            />

            <Select
              value={sortOrder}
              onChange={(value) => setSortOrder((value as SortOrder) ?? 'asc')}
              data={[
                { value: 'asc', label: t('users.sortAZ') },
                { value: 'desc', label: t('users.sortZA') },
              ]}
              allowDeselect={false}
              style={{ minWidth: 220 }}
            />
          </Group>
        </Group>

        {loading ? (
          <Group justify="center" py="xl">
            <Loader />
          </Group>
        ) : users.length === 0 ? (
          <Paper withBorder radius="md" p="xl">
            <Stack align="center" gap={6}>
              <Text fw={600}>{t('users.emptyTitle')}</Text>
              <Text c="dimmed" ta="center">
                {t('users.emptyDescription')}
              </Text>
            </Stack>
          </Paper>
        ) : (
          <>
            <SimpleGrid cols={{ base: 1, sm: 2, md: 3, xl: 4 }} spacing="md">
              {users.map((user) => (
                <Card
                  key={user.id}
                  withBorder
                  radius="lg"
                  p="md"
                  className={styles.hoverCard}
                  onClick={() => void openEditModal(user)}
                  style={{ cursor: 'pointer' }}
                >
                  <Group align="flex-start" justify="space-between" wrap="nowrap" gap="sm">
                    <Group gap="sm" wrap="nowrap" style={{ minWidth: 0, flex: 1 }}>
                      <Avatar size={40} radius="xl" color="blue">
                        {getInitials(user.name)}
                      </Avatar>

                      <Stack gap={2} style={{ minWidth: 0, flex: 1 }}>
                        <Text fw={700} style={{ whiteSpace: 'normal', overflowWrap: 'anywhere' }}>
                          {user.name}
                        </Text>

                        <Group gap={4} wrap="nowrap" style={{ minWidth: 0 }}>
                          <IconMail size={13} color="var(--mantine-color-dimmed)" />

                          <Tooltip label={user.email} withArrow openDelay={300}>
                            <Text size="xs" c="dimmed" truncate>
                              {user.email}
                            </Text>
                          </Tooltip>
                        </Group>
                      </Stack>
                    </Group>

                    <Tooltip label={t('common.delete')}>
                      <ActionIcon
                        variant="light"
                        color="red"
                        size="md"
                        onClick={(event) => {
                          event.stopPropagation();
                          setDeletingUser(user);
                        }}
                      >
                        <IconTrash size={16} />
                      </ActionIcon>
                    </Tooltip>
                  </Group>

                  <Divider mt="sm" />

                  <Group justify="space-between" align="center" mt="sm" wrap="nowrap">
                    <Group gap="xs" wrap="nowrap">
                      <Box onClick={(event) => event.stopPropagation()}>
                        <Switch
                          size="sm"
                          checked={user.enabled}
                          onChange={(event) =>
                            void toggleEnabled(user, event.currentTarget.checked)
                          }
                        />
                      </Box>

                      <Badge color={user.enabled ? 'green' : 'gray'} variant="light">
                        {user.enabled ? t('users.enabled') : t('users.disabled')}
                      </Badge>
                    </Group>

                    <Text size="xs" c="dimmed">
                      {formatDate(user.createdAt)}
                    </Text>
                  </Group>
                </Card>
              ))}
            </SimpleGrid>

            <Group justify="space-between" align="center" wrap="wrap" gap="sm">
              <Text size="sm" c="dimmed">
                {t('users.showing', {
                  from: pagination.total === 0 ? 0 : (page - 1) * pageSize + 1,
                  to: Math.min(page * pageSize, pagination.total),
                  total: pagination.total,
                })}
              </Text>

              <Group gap="xs">
                <Pagination total={pagination.totalPages} value={page} onChange={setPage} />
                <Select
                  value={String(pageSize)}
                  onChange={(value) => setPageSize(Number(value) || PAGE_SIZE)}
                  data={[12, 24, 48].map((size) => ({ value: String(size), label: `${size}` }))}
                  aria-label={t('users.pageSize')}
                  allowDeselect={false}
                  style={{ width: 90 }}
                />
              </Group>
            </Group>
          </>
        )}
      </Stack>
    </>
  );
}
