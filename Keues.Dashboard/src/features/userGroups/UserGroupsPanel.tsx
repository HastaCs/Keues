import {
  ActionIcon,
  Alert,
  Button,
  Card,
  Divider,
  Group,
  Loader,
  Modal,
  Paper,
  SegmentedControl,
  Select,
  SimpleGrid,
  Stack,
  Table,
  Text,
  TextInput,
  ThemeIcon,
  Tooltip,
} from '@mantine/core';
import {
  IconArrowsSort,
  IconChevronDown,
  IconChevronUp,
  IconLayoutGrid,
  IconSearch,
  IconTable,
  IconTrash,
  IconUsers,
  IconUsersGroup,
} from '@tabler/icons-react';
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import { ApiError } from '@/api/httpClient';
import { userGroupsApi } from '@/api/UserGroupsApi';
import type { UserGroup } from '@/api/interfaces/UserGroup/UserGroups';
import { EmptyState } from '@/components/EmptyState/EmptyState';
import { PageHeader } from '@/components/PageHeader/PageHeader';
import { useActiveLocation } from '@/features/locations/LocationContext';
import styles from '@/styles/hover-card.module.css';

type UserGroupView = 'cards' | 'table';

type UserGroupSortField = 'createdAt' | 'name';

interface UserGroupSort {
  field: UserGroupSortField;
  direction: 'asc' | 'desc';
}

const SORT_STORAGE_KEY = 'keues.userGroups.sort';
const VIEW_STORAGE_KEY = 'keues.userGroups.view';

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

function getStoredView(): UserGroupView {
  return window.localStorage.getItem(VIEW_STORAGE_KEY) === 'table' ? 'table' : 'cards';
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

function formatDate(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '-';
  }

  return date.toLocaleDateString();
}

function SortableTh({
  field,
  sort,
  onSort,
  children,
}: {
  field: UserGroupSortField;
  sort: UserGroupSort;
  onSort: (field: UserGroupSortField) => void;
  children: ReactNode;
}) {
  const active = sort.field === field;

  return (
    <Table.Th>
      <Group
        gap={4}
        wrap="nowrap"
        onClick={() => onSort(field)}
        style={{ cursor: 'pointer', userSelect: 'none' }}
      >
        {children}
        {active ? (
          sort.direction === 'asc' ? (
            <IconChevronUp size={14} />
          ) : (
            <IconChevronDown size={14} />
          )
        ) : (
          <IconArrowsSort size={14} style={{ opacity: 0.35 }} />
        )}
      </Group>
    </Table.Th>
  );
}

function UsersCount({ group }: { group: UserGroup }) {
  const { t } = useTranslation();

  return (
    <Group gap={6} wrap="nowrap">
      <IconUsers size={14} color="var(--mantine-color-dimmed)" />

      <Tooltip
        disabled={group.userIds.length === 0}
        withArrow
        openDelay={200}
        label={
          <Stack gap={2}>
            {group.userIds.map((user) => (
              <Text key={user.id} size="xs">
                {user.name}
              </Text>
            ))}
          </Stack>
        }
      >
        <Text size="xs" c="dimmed">
          {t('groups.userCount', { count: group.userIds.length })}
        </Text>
      </Tooltip>
    </Group>
  );
}

export function UserGroupsPanel() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useActiveLocation();

  const [groups, setGroups] = useState<UserGroup[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [view, setView] = useState<UserGroupView>(getStoredView);
  const [sort, setSort] = useState<UserGroupSort>(getStoredSort);
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

  function handleViewChange(value: string) {
    const next = value as UserGroupView;
    setView(next);
    window.localStorage.setItem(VIEW_STORAGE_KEY, next);
  }

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

  function handleSortToggle(field: UserGroupSortField) {
    setSort((previous) => {
      const next: UserGroupSort =
        previous.field === field
          ? { field, direction: previous.direction === 'asc' ? 'desc' : 'asc' }
          : { field, direction: 'asc' };

      persistSort(next);

      return next;
    });
  }

  function openCreatePage() {
    if (location) {
      navigate(`/locations/${location.id}/groups/new`);
    }
  }

  function openEditPage(group: UserGroup) {
    if (location) {
      navigate(`/locations/${location.id}/groups/${group.id}`);
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

      <Stack gap="lg">
        <PageHeader
          label={location.name}
          title={t('groups.heading')}
          actions={
            groups.length > 0 ? (
              <Button onClick={openCreatePage}>{t('groups.newGroup')}</Button>
            ) : undefined
          }
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

          <Group gap="sm" align="flex-end">
            <SegmentedControl
              value={view}
              onChange={handleViewChange}
              data={[
                {
                  value: 'cards',
                  label: (
                    <Group gap={6} wrap="nowrap">
                      <IconLayoutGrid size={14} />
                      {t('groups.viewCards')}
                    </Group>
                  ),
                },
                {
                  value: 'table',
                  label: (
                    <Group gap={6} wrap="nowrap">
                      <IconTable size={14} />
                      {t('groups.viewTable')}
                    </Group>
                  ),
                },
              ]}
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
        </Group>

        {loading ? (
          <Group justify="center" py="xl">
            <Loader />
          </Group>
        ) : filteredGroups.length === 0 ? (
          <EmptyState
            title={t('groups.emptyTitle')}
            description={t('groups.emptyDescription')}
            action={
              groups.length === 0 ? (
                <Button onClick={openCreatePage}>{t('groups.newGroup')}</Button>
              ) : undefined
            }
          />
        ) : view === 'cards' ? (
          <SimpleGrid cols={{ base: 1, sm: 2, md: 3, xl: 4 }} spacing="md">
            {filteredGroups.map((group) => (
              <Card
                key={group.id}
                withBorder
                radius="lg"
                p="md"
                className={styles.hoverCard}
                onClick={() => openEditPage(group)}
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

                <Divider mt="sm" />

                <Group mt="sm">
                  <UsersCount group={group} />
                </Group>
              </Card>
            ))}
          </SimpleGrid>
        ) : (
          <Paper withBorder radius="md" p="sm" style={{ overflowX: 'auto' }}>
            <Table highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <SortableTh field="name" sort={sort} onSort={handleSortToggle}>
                    {t('groups.name')}
                  </SortableTh>
                  <Table.Th>{t('groups.users')}</Table.Th>
                  <SortableTh field="createdAt" sort={sort} onSort={handleSortToggle}>
                    {t('groups.createdAt')}
                  </SortableTh>
                  <Table.Th>{t('groups.actions')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {filteredGroups.map((group) => (
                  <Table.Tr
                    key={group.id}
                    style={{ cursor: 'pointer' }}
                    onClick={() => openEditPage(group)}
                  >
                    <Table.Td>
                      <Group gap="xs" wrap="nowrap" style={{ minWidth: 0 }}>
                        <ThemeIcon size={28} radius="xl" color={group.color} variant="light">
                          <IconUsersGroup size={14} />
                        </ThemeIcon>

                        <Text fw={600} truncate>
                          {group.name}
                        </Text>
                      </Group>
                    </Table.Td>

                    <Table.Td>
                      <UsersCount group={group} />
                    </Table.Td>

                    <Table.Td>
                      <Text size="sm" c="dimmed">
                        {formatDate(group.createdAt)}
                      </Text>
                    </Table.Td>

                    <Table.Td>
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
                          <IconTrash size={16} />
                        </ActionIcon>
                      </Tooltip>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </Paper>
        )}
      </Stack>
    </>
  );
}
