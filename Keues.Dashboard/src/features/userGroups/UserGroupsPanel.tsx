import {
  ActionIcon,
  Alert,
  Button,
  Card,
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
import { IconSearch, IconTrash, IconUsersGroup } from '@tabler/icons-react';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { ApiError } from '@/api/httpClient';
import { userGroupsApi } from '@/api/UserGroupsApi';
import type { CreateUserGroupInput, UserGroup } from '@/api/interfaces/UserGroup/UserGroups';
import { PageHeader } from '@/components/PageHeader/PageHeader';
import { useActiveLocation } from '@/features/locations/LocationContext';
import styles from '@/styles/hover-card.module.css';
import { UserGroupFormModal } from './UserGroupFormModal';

type UserGroupSortField = 'createdAt' | 'name';

interface UserGroupSort {
  field: UserGroupSortField;
  direction: 'asc' | 'desc';
}

const SORT_STORAGE_KEY = 'keues.userGroups.sort';

const DEFAULT_SORT: UserGroupSort = { field: 'createdAt', direction: 'desc' };

function getStoredSort(): UserGroupSort {
  const stored = window.localStorage.getItem(SORT_STORAGE_KEY);

  if (stored) {
    const [field, direction] = stored.split(':');

    if (
      (field === 'createdAt' || field === 'name') &&
      (direction === 'asc' || direction === 'desc')
    ) {
      return { field, direction };
    }

    if (stored === 'asc') {
      return { field: 'name', direction: 'asc' };
    }

    if (stored === 'desc') {
      return { field: 'name', direction: 'desc' };
    }
  }

  return DEFAULT_SORT;
}

function persistSort(sort: UserGroupSort) {
  window.localStorage.setItem(SORT_STORAGE_KEY, `${sort.field}:${sort.direction}`);
}

function sortGroups(items: UserGroup[], sort: UserGroupSort): UserGroup[] {
  const sorted = [...items].sort((left, right) => {
    if (sort.field === 'createdAt') {
      return new Date(left.createdAt).getTime() - new Date(right.createdAt).getTime();
    }

    return left.name.localeCompare(right.name, 'es');
  });

  return sort.direction === 'desc' ? sorted.reverse() : sorted;
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

export function UserGroupsPanel() {
  const { t } = useTranslation();
  const location = useActiveLocation();

  const [groups, setGroups] = useState<UserGroup[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [sort, setSort] = useState<UserGroupSort>(getStoredSort);
  const [formOpened, setFormOpened] = useState(false);
  const [editingGroup, setEditingGroup] = useState<UserGroup | undefined>(undefined);
  const [deletingGroup, setDeletingGroup] = useState<UserGroup | undefined>(undefined);
  const [deleting, setDeleting] = useState(false);

  const refreshGroups = useCallback(async () => {
    if (!location) {
      return;
    }

    setError(null);
    setLoading(true);

    try {
      const response = await userGroupsApi.list(location.id);
      setGroups(response.data);
    } catch (requestError) {
      setError(getErrorMessage(requestError, t('errors.unexpected')));
    } finally {
      setLoading(false);
    }
  }, [location, t]);

  useEffect(() => {
    void refreshGroups();
  }, [refreshGroups]);

  const filteredGroups = useMemo(() => {
    const query = search.trim().toLowerCase();
    const matches = query
      ? groups.filter((group) => group.name.toLowerCase().includes(query))
      : groups;

    return sortGroups(matches, sort);
  }, [groups, search, sort]);

  function handleSortChange(value: string | null) {
    const next: UserGroupSort =
      value === 'name:asc'
        ? { field: 'name', direction: 'asc' }
        : value === 'name:desc'
          ? { field: 'name', direction: 'desc' }
          : DEFAULT_SORT;

    setSort(next);
    persistSort(next);
  }

  function openCreateModal() {
    setEditingGroup(undefined);
    setFormError(null);
    setFormOpened(true);
  }

  function openEditModal(group: UserGroup) {
    setEditingGroup(group);
    setFormError(null);
    setFormOpened(true);
  }

  async function handleSubmitGroup(payload: CreateUserGroupInput) {
    setSaving(true);
    setFormError(null);

    try {
      if (editingGroup) {
        await userGroupsApi.update({ id: editingGroup.id, ...payload });
      } else {
        await userGroupsApi.create(payload);
      }

      setFormOpened(false);
      setEditingGroup(undefined);
      setFormError(null);
      await refreshGroups();
    } catch (requestError) {
      setFormError(getErrorMessage(requestError, t('errors.unexpected')));
    } finally {
      setSaving(false);
    }
  }

  async function handleConfirmDeleteGroup() {
    if (!deletingGroup) {
      return;
    }

    setDeleting(true);
    setError(null);

    try {
      await userGroupsApi.remove(deletingGroup.id);
      setDeletingGroup(undefined);
      await refreshGroups();
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
        opened={Boolean(deletingGroup)}
        onClose={() => setDeletingGroup(undefined)}
        title={t('groups.deleteTitle')}
        centered
      >
        <Stack gap="lg">
          <Text>{t('groups.deleteDescription', { name: deletingGroup?.name ?? '' })}</Text>

          <Group justify="flex-end">
            <Button
              variant="default"
              onClick={() => setDeletingGroup(undefined)}
              disabled={deleting}
            >
              {t('common.cancel')}
            </Button>

            <Button
              color="red"
              leftSection={<IconTrash size={14} />}
              loading={deleting}
              onClick={handleConfirmDeleteGroup}
            >
              {t('common.delete')}
            </Button>
          </Group>
        </Stack>
      </Modal>

      <UserGroupFormModal
        opened={formOpened}
        loading={saving}
        error={formError}
        initialGroup={editingGroup}
        locationId={location.id}
        onClose={() => {
          setFormOpened(false);
          setFormError(null);
        }}
        onSubmit={handleSubmitGroup}
      />

      <Stack gap="lg">
        <PageHeader
          label={t('groups.title')}
          title={location.name}
          description={t('groups.subtitle')}
          actions={<Button onClick={openCreateModal}>{t('groups.newGroup')}</Button>}
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
            placeholder={t('groups.searchPlaceholder')}
            leftSection={<IconSearch size={16} />}
            style={{ maxWidth: 420, width: '100%' }}
          />

          <Select
            value={`${sort.field}:${sort.direction}`}
            onChange={handleSortChange}
            data={[
              { value: 'createdAt:desc', label: t('groups.sortCreated') },
              { value: 'name:asc', label: t('groups.sortAZ') },
              { value: 'name:desc', label: t('groups.sortZA') },
            ]}
            allowDeselect={false}
            style={{ minWidth: 220 }}
          />
        </Group>

        {loading ? (
          <Group justify="center" py="xl">
            <Loader />
          </Group>
        ) : filteredGroups.length === 0 ? (
          <Paper withBorder radius="md" p="xl">
            <Stack align="center" gap={6}>
              <Text fw={600}>{t('groups.emptyTitle')}</Text>
              <Text c="dimmed" ta="center">
                {t('groups.emptyDescription')}
              </Text>
            </Stack>
          </Paper>
        ) : (
          <SimpleGrid cols={{ base: 1, sm: 2, md: 3, xl: 4 }} spacing="md">
            {filteredGroups.map((group) => (
              <Card
                key={group.id}
                withBorder
                radius="lg"
                p="md"
                className={styles.hoverCard}
                onClick={() => openEditModal(group)}
                style={{
                  borderLeft: `6px solid var(--mantine-color-${group.color}-6)`,
                  cursor: 'pointer',
                }}
              >
                <Group justify="space-between" align="flex-start" wrap="nowrap">
                  <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
                    <ThemeIcon size={36} radius="xl" color={group.color} variant="light">
                      <IconUsersGroup size={18} />
                    </ThemeIcon>

                    <Text fw={700} truncate>
                      {group.name}
                    </Text>
                  </Group>

                  <Tooltip label={t('common.delete')}>
                    <ActionIcon
                      variant="light"
                      color="red"
                      size="md"
                      onClick={(event) => {
                        event.stopPropagation();
                        setDeletingGroup(group);
                      }}
                    >
                      <IconTrash size={18} />
                    </ActionIcon>
                  </Tooltip>
                </Group>
              </Card>
            ))}
          </SimpleGrid>
        )}
      </Stack>
    </>
  );
}
