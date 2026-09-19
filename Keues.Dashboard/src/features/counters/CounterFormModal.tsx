import {
  Button,
  Card,
  ColorSwatch,
  Group,
  Modal,
  SimpleGrid,
  Stack,
  Switch,
  Tabs,
  Text,
  Textarea,
  TextInput,
  ThemeIcon,
  Tooltip,
} from '@mantine/core';

import {
  IconCheck,
  IconDeviceTv,
  IconSearch,
  IconTicket,
  IconUser,
  IconUsersGroup,
} from '@tabler/icons-react';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { queuesApi } from '@/api/QueuesApi';
import { usersApi } from '@/api/UsersApi';
import { userGroupsApi } from '@/api/UserGroupsApi';
import type { Counter, CreateCounterInput } from '@/api/interfaces/Counter/Counters';
import type { Queue } from '@/api/interfaces/Queue/Queues';
import type { User } from '@/api/interfaces/User/Users';
import type { UserGroup } from '@/api/interfaces/UserGroup/UserGroups';
import { colors } from '@/data/common';

const AUTHORIZED_USERS_LIMIT = 100;

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
  color: string;
}

function getInitialState(initialCounter?: Counter): CounterFormState {
  return {
    code: initialCounter?.code ?? '',
    name: initialCounter?.name ?? '',
    description: initialCounter?.description ?? '',
    color: initialCounter?.color ?? 'blue',
  };
}

export function CounterFormModal(props: CounterFormModalProps) {
  const { t } = useTranslation();

  const { opened, loading, initialCounter, locationId, onClose, onSubmit } = props;

  const [formState, setFormState] = useState<CounterFormState>(getInitialState(initialCounter));

  const [queues, setQueues] = useState<Queue[]>([]);

  const [selectedQueues, setSelectedQueues] = useState<string[]>([]);

  const [userGroups, setUserGroups] = useState<UserGroup[]>([]);

  const [selectedUserGroups, setSelectedUserGroups] = useState<string[]>([]);

  const [users, setUsers] = useState<User[]>([]);

  const [selectedUsers, setSelectedUsers] = useState<string[]>([]);

  const [userSearch, setUserSearch] = useState('');
  const [showOnlyAuthorizedUsers, setShowOnlyAuthorizedUsers] = useState(false);

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
        // Ignoramos errores al cargar las colas.
      }
    }

    async function loadUserGroups() {
      try {
        const response = await userGroupsApi.list(locationId);

        setUserGroups(response.data);
      } catch {
        // Ignoramos errores al cargar los grupos.
      }
    }

    async function loadUsers() {
      try {
        const response = await usersApi.list({
          locationId,
          name: '',
          isActive: true,
          page: 1,
          limit: AUTHORIZED_USERS_LIMIT,
        });

        setUsers([...response.data].sort((a, b) => a.name.localeCompare(b.name, 'es')));
      } catch {
        // Ignoramos errores al cargar los usuarios.
      }
    }

    loadQueues();
    loadUserGroups();
    loadUsers();
  }, [opened, locationId]);

  useEffect(() => {
    if (!opened) {
      return;
    }

    setFormState(getInitialState(initialCounter));

    setSelectedQueues(initialCounter?.queues ?? []);

    setSelectedUserGroups(initialCounter?.authorizedUserGroups.map((group) => group.id) ?? []);

    setSelectedUsers(initialCounter?.authorizedUsers.map((user) => user.id) ?? []);

    setUserSearch('');

    setCodeError(null);
    setNameError(null);
  }, [opened, initialCounter]);

  // Grupos seleccionados -> qué usuarios cubren (para marcarlos como "ya incluidos por grupo").
  const coveredUserIds = useMemo(() => {
    const map = new Map<string, { name: string; color: string }>();

    userGroups
      .filter((group) => selectedUserGroups.includes(group.id))
      .forEach((group) => {
        group.userIds.forEach((user) => {
          if (!map.has(user.id)) {
            map.set(user.id, { name: group.name, color: group.color });
          }
        });
      });

    return map;
  }, [userGroups, selectedUserGroups]);

  const filteredUsers = useMemo(() => {
    const query = userSearch.trim().toLowerCase();

    return users.filter((user) => {
      if (
        showOnlyAuthorizedUsers &&
        !selectedUsers.includes(user.id) &&
        !coveredUserIds.has(user.id)
      ) {
        return false;
      }

      return query.length === 0 || user.name.toLowerCase().includes(query);
    });
  }, [users, userSearch, showOnlyAuthorizedUsers, selectedUsers, coveredUserIds]);

  function toggleQueue(queueId: string) {
    setSelectedQueues((current) => {
      if (current.includes(queueId)) {
        return current.filter((id) => id !== queueId);
      }

      return [...current, queueId];
    });
  }

  function toggleUserGroup(groupId: string) {
    setSelectedUserGroups((current) => {
      if (current.includes(groupId)) {
        return current.filter((id) => id !== groupId);
      }

      return [...current, groupId];
    });
  }

  function toggleUser(userId: string) {
    setSelectedUsers((current) => {
      if (current.includes(userId)) {
        return current.filter((id) => id !== userId);
      }

      return [...current, userId];
    });
  }

  const isEditing = Boolean(initialCounter);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const normalizedCode = formState.code.trim().toUpperCase();

    const normalizedName = formState.name.trim();

    if (normalizedCode.length === 0) {
      setCodeError(t('counterForm.codeRequired'));

      return;
    }

    if (normalizedName.length === 0) {
      setNameError(t('counterForm.nameRequired'));

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
      authorizedUsers: selectedUsers,
      authorizedUserGroups: selectedUserGroups,
    });
  }

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={isEditing ? t('counterForm.editTitle') : t('counterForm.createTitle')}
      centered
      size="lg"
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <Tabs defaultValue="general" keepMounted={false}>
            <Tabs.List>
              <Tabs.Tab value="general">{t('counterForm.tabGeneral')}</Tabs.Tab>
              <Tabs.Tab value="queues">{t('counterForm.tabQueues')}</Tabs.Tab>
              <Tabs.Tab value="groups">{t('counterForm.tabGroups')}</Tabs.Tab>
              <Tabs.Tab value="users">{t('counterForm.tabUsers')}</Tabs.Tab>
            </Tabs.List>

            <Tabs.Panel
              value="general"
              pt="md"
              h="max(280px, min(460px, calc(100dvh - 260px)))"
              style={{ overflowY: 'auto' }}
            >
              <Stack gap="md">
                <Stack gap={4}>
                  <Text fw={600}>{t('counterForm.code')}</Text>

                  <TextInput
                    placeholder={t('counterForm.codePlaceholder')}
                    leftSection={<IconDeviceTv size={16} />}
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
                </Stack>

                <Stack gap={4}>
                  <Text fw={600}>{t('counterForm.name')}</Text>

                  <TextInput
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
                </Stack>

                <Stack gap={4}>
                  <Text fw={600}>{t('counterForm.description')}</Text>

                  <Textarea
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
                </Stack>

                <Stack gap="xs">
                  <Text fw={600}>{t('counterForm.color')}</Text>

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
              </Stack>
            </Tabs.Panel>

            <Tabs.Panel
              value="queues"
              pt="md"
              h="max(280px, min(460px, calc(100dvh - 260px)))"
              style={{ overflowY: 'auto' }}
            >
              <Stack gap="sm">
                <Text fw={600}>{t('counterForm.allowedTicketTypes')}</Text>

                <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
                  {queues.map((queue) => {
                    const enabled = selectedQueues.includes(queue.id);

                    return (
                      <Card
                        key={queue.id}
                        withBorder
                        padding="sm"
                        radius="md"
                        style={{
                          cursor: 'pointer',
                          borderColor: enabled ? 'var(--mantine-color-blue-5)' : undefined,
                          backgroundColor: enabled ? 'var(--mantine-color-blue-0)' : undefined,
                        }}
                        onClick={() => toggleQueue(queue.id)}
                      >
                        <Group justify="space-between" wrap="nowrap">
                          <Group gap="sm">
                            <ThemeIcon
                              size={34}
                              radius="md"
                              variant={enabled ? 'light' : 'default'}
                              color={enabled ? 'blue' : 'gray'}
                            >
                              <IconTicket size={18} />
                            </ThemeIcon>

                            <div>
                              <Text size="sm" fw={600}>
                                {queue.name}
                              </Text>

                              <Text size="xs" c="dimmed">
                                {queue.code}
                              </Text>
                            </div>
                          </Group>

                          <div onClick={(event) => event.stopPropagation()}>
                            <Switch checked={enabled} onChange={() => toggleQueue(queue.id)} />
                          </div>
                        </Group>
                      </Card>
                    );
                  })}
                </SimpleGrid>
              </Stack>
            </Tabs.Panel>

            <Tabs.Panel
              value="groups"
              pt="md"
              h="max(280px, min(460px, calc(100dvh - 260px)))"
              style={{ overflowY: 'auto' }}
            >
              <Stack gap="sm">
                <Text fw={600}>{t('counterForm.authorizedGroups')}</Text>

                <Text size="xs" c="dimmed">
                  {t('counterForm.noGroupsMeansAll')}
                </Text>

                {userGroups.length === 0 ? (
                  <Text size="sm" c="dimmed">
                    {t('counterForm.noGroups')}
                  </Text>
                ) : (
                  <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
                    {userGroups.map((group) => {
                      const enabled = selectedUserGroups.includes(group.id);

                      return (
                        <Card
                          key={group.id}
                          withBorder
                          padding="sm"
                          radius="md"
                          style={{
                            cursor: 'pointer',
                            borderColor: enabled
                              ? `var(--mantine-color-${group.color}-5)`
                              : undefined,
                            backgroundColor: enabled ? 'var(--mantine-color-gray-0)' : undefined,
                          }}
                          onClick={() => toggleUserGroup(group.id)}
                        >
                          <Group justify="space-between" wrap="nowrap">
                            <Group gap="sm">
                              <ThemeIcon
                                size={34}
                                radius="md"
                                variant={enabled ? 'light' : 'default'}
                                color={enabled ? group.color : 'gray'}
                              >
                                <IconUsersGroup size={18} />
                              </ThemeIcon>

                              <Text size="sm" fw={600}>
                                {group.name}
                              </Text>
                            </Group>

                            <div onClick={(event) => event.stopPropagation()}>
                              <Switch
                                checked={enabled}
                                onChange={() => toggleUserGroup(group.id)}
                              />
                            </div>
                          </Group>
                        </Card>
                      );
                    })}
                  </SimpleGrid>
                )}
              </Stack>
            </Tabs.Panel>

            <Tabs.Panel
              value="users"
              pt="md"
              h="max(280px, min(460px, calc(100dvh - 260px)))"
              style={{ overflowY: 'auto' }}
            >
              <Stack gap="sm">
                <Text fw={600}>{t('counterForm.authorizedUsers')}</Text>

                <Text size="xs" c="dimmed">
                  {t('counterForm.authorizedUsersHelp')}
                </Text>

                <TextInput
                  value={userSearch}
                  onChange={(event) => setUserSearch(event.currentTarget.value)}
                  placeholder={t('counterForm.searchUsersPlaceholder')}
                  leftSection={<IconSearch size={16} />}
                />

                <Switch
                  label={t('counterForm.onlyAuthorizedUsers')}
                  checked={showOnlyAuthorizedUsers}
                  onChange={(event) => setShowOnlyAuthorizedUsers(event.currentTarget.checked)}
                />

                {users.length === 0 ? (
                  <Text size="sm" c="dimmed">
                    {t('counterForm.noActiveUsers')}
                  </Text>
                ) : filteredUsers.length === 0 ? (
                  <Text size="sm" c="dimmed">
                    {t('counterForm.noUsersFound')}
                  </Text>
                ) : (
                  <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
                    {filteredUsers.map((user) => {
                      const enabled = selectedUsers.includes(user.id);
                      const coveredByGroup = coveredUserIds.get(user.id);

                      return (
                        <Card
                          key={user.id}
                          withBorder
                          padding="sm"
                          radius="md"
                          style={{
                            cursor: 'pointer',
                            borderColor: enabled ? 'var(--mantine-color-blue-5)' : undefined,
                            backgroundColor: enabled ? 'var(--mantine-color-blue-0)' : undefined,
                          }}
                          onClick={() => toggleUser(user.id)}
                        >
                          <Group justify="space-between" wrap="nowrap">
                            <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
                              <ThemeIcon
                                size={34}
                                radius="md"
                                variant={enabled ? 'light' : 'default'}
                                color={enabled ? 'blue' : 'gray'}
                              >
                                <IconUser size={18} />
                              </ThemeIcon>

                              <Stack gap={0} style={{ minWidth: 0 }}>
                                <Text size="sm" fw={600} truncate>
                                  {user.name}
                                </Text>

                                {coveredByGroup ? (
                                  <Group gap={4} wrap="nowrap">
                                    <IconUsersGroup
                                      size={12}
                                      color={`var(--mantine-color-${coveredByGroup.color}-6)`}
                                    />
                                    <Text size="xs" c={coveredByGroup.color} truncate>
                                      {t('counterForm.coveredByGroup', {
                                        group: coveredByGroup.name,
                                      })}
                                    </Text>
                                  </Group>
                                ) : null}
                              </Stack>
                            </Group>

                            {coveredByGroup ? (
                              <Tooltip
                                label={t('counterForm.coveredByGroupTooltip', {
                                  group: coveredByGroup.name,
                                })}
                                withArrow
                              >
                                <ThemeIcon size={22} radius="xl" variant="light" color="blue">
                                  <IconCheck size={14} />
                                </ThemeIcon>
                              </Tooltip>
                            ) : (
                              <div onClick={(event) => event.stopPropagation()}>
                                <Switch checked={enabled} onChange={() => toggleUser(user.id)} />
                              </div>
                            )}
                          </Group>
                        </Card>
                      );
                    })}
                  </SimpleGrid>
                )}
              </Stack>
            </Tabs.Panel>
          </Tabs>

          <Group justify="flex-end">
            <Button variant="default" onClick={onClose} disabled={loading}>
              {t('common.cancel')}
            </Button>

            <Button type="submit" loading={loading}>
              {isEditing ? t('counterForm.editAction') : t('counterForm.createAction')}
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
