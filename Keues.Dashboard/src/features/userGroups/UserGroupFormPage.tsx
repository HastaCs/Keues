import {
  Alert,
  Badge,
  Button,
  ColorSwatch,
  Grid,
  Group,
  Loader,
  Paper,
  ScrollArea,
  SimpleGrid,
  Stack,
  Text,
  TextInput,
} from '@mantine/core';
import { IconCheck, IconSearch, IconX } from '@tabler/icons-react';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';

import { ApiError } from '@/api/httpClient';
import { usersApi } from '@/api/UsersApi';
import { userGroupsApi } from '@/api/UserGroupsApi';
import type { User } from '@/api/interfaces/User/Users';
import type { CreateUserGroupInput, UserGroupUser } from '@/api/interfaces/UserGroup/UserGroups';
import { PageHeader } from '@/components/PageHeader/PageHeader';
import { useActiveLocation } from '@/features/locations/LocationContext';
import { colors } from '@/data/common';

const USERS_LIMIT = 100;

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

export function UserGroupFormPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useActiveLocation();
  const { groupId } = useParams();
  const isEditing = Boolean(groupId);

  const [name, setName] = useState('');
  const [color, setColor] = useState('blue');
  const [nameError, setNameError] = useState<string | null>(null);
  const [selectedUsers, setSelectedUsers] = useState<UserGroupUser[]>([]);
  const [availableUsers, setAvailableUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [userSearch, setUserSearch] = useState('');

  useEffect(() => {
    if (!location) {
      return;
    }

    let cancelled = false;

    setLoading(true);
    setError(null);

    const groupRequest = isEditing && groupId ? userGroupsApi.get(groupId) : Promise.resolve(null);

    Promise.all([
      groupRequest,
      usersApi.list({
        locationId: location.id,
        name: '',
        isActive: true,
        page: 1,
        limit: USERS_LIMIT,
      }),
    ])
      .then(([group, usersResponse]) => {
        if (cancelled) {
          return;
        }

        if (group) {
          setName(group.name);
          setColor(group.color);
          setSelectedUsers(group.userIds);
        }

        setAvailableUsers(usersResponse.data);
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
  }, [location, groupId, isEditing, t]);

  const selectedIds = useMemo(() => new Set(selectedUsers.map((user) => user.id)), [selectedUsers]);

  const filteredUsers = useMemo(() => {
    const query = userSearch.trim().toLowerCase();

    return availableUsers.filter(
      (user) =>
        !selectedIds.has(user.id) && (query.length === 0 || user.name.toLowerCase().includes(query))
    );
  }, [availableUsers, selectedIds, userSearch]);

  function addUser(user: User) {
    setSelectedUsers((previous) => [...previous, { id: user.id, name: user.name }]);
    setUserSearch('');
  }

  function removeUser(id: string) {
    setSelectedUsers((previous) => previous.filter((user) => user.id !== id));
  }

  function goBack() {
    if (location) {
      navigate(`/locations/${location.id}/groups`);
    }
  }

  async function handleSave() {
    if (!location) {
      return;
    }

    const trimmedName = name.trim();

    if (trimmedName.length === 0) {
      setNameError(t('userGroupForm.nameRequired'));
      return;
    }

    setNameError(null);
    setSaving(true);
    setError(null);

    const payload: CreateUserGroupInput = {
      name: trimmedName,
      color,
      locationId: location.id,
      userIds: selectedUsers.map((user) => user.id),
    };

    try {
      if (isEditing && groupId) {
        await userGroupsApi.update({ id: groupId, ...payload });
      } else {
        await userGroupsApi.create(payload);
      }

      goBack();
    } catch (requestError) {
      setError(getErrorMessage(requestError, t('errors.unexpected')));
    } finally {
      setSaving(false);
    }
  }

  if (!location) {
    return null;
  }

  return (
    <Stack gap="lg">
      <PageHeader
        label={t('groups.title')}
        title={isEditing ? t('userGroupForm.editTitle') : t('userGroupForm.createTitle')}
        description={location.name}
        actions={
          <Group>
            <Button variant="default" onClick={goBack} disabled={saving}>
              {t('common.cancel')}
            </Button>

            <Button onClick={() => void handleSave()} loading={saving}>
              {t('userGroupForm.saveAction')}
            </Button>
          </Group>
        }
      />

      {error ? (
        <Alert color="red" title={t('errors.requestFailed')}>
          {error}
        </Alert>
      ) : null}

      {loading ? (
        <Group justify="center" py="xl">
          <Loader />
        </Group>
      ) : (
        <Grid gap="lg">
          <Grid.Col span={{ base: 12, md: 4 }}>
            <Paper withBorder radius="md" p="lg">
              <Stack gap="md">
                <TextInput
                  label={t('userGroupForm.name')}
                  placeholder={t('userGroupForm.namePlaceholder')}
                  value={name}
                  error={nameError}
                  withAsterisk
                  onChange={(event) => setName(event.currentTarget.value)}
                />

                <Stack gap="xs">
                  <Text fw={500}>{t('userGroupForm.color')}</Text>

                  <SimpleGrid cols={6} spacing="sm">
                    {colors.map((option) => (
                      <ColorSwatch
                        key={option}
                        color={`var(--mantine-color-${option}-6)`}
                        style={{
                          cursor: 'pointer',
                          borderRadius: '50%',
                          border:
                            color === option
                              ? '2px solid var(--mantine-color-black)'
                              : '2px solid transparent',
                        }}
                        onClick={() => setColor(option)}
                      >
                        {color === option && <IconCheck size={14} color="white" />}
                      </ColorSwatch>
                    ))}
                  </SimpleGrid>
                </Stack>
              </Stack>
            </Paper>
          </Grid.Col>

          <Grid.Col span={{ base: 12, md: 8 }}>
            <Grid gap="md">
              <Grid.Col span={{ base: 12, md: 6 }}>
                <Paper withBorder radius="md" p="lg" h="100%">
                  <Stack gap="sm">
                    <Text fw={600}>{t('userGroupForm.availableUsers')}</Text>

                    <TextInput
                      value={userSearch}
                      onChange={(event) => setUserSearch(event.currentTarget.value)}
                      placeholder={t('userGroupForm.searchUsersPlaceholder')}
                      leftSection={<IconSearch size={16} />}
                    />

                    {availableUsers.length === 0 ? (
                      <Text size="sm" c="dimmed">
                        {t('userGroupForm.noActiveUsers')}
                      </Text>
                    ) : filteredUsers.length === 0 ? (
                      <Text size="sm" c="dimmed">
                        {t('userGroupForm.noUsers')}
                      </Text>
                    ) : (
                      <ScrollArea h={420} type="auto">
                        <Stack gap={4}>
                          {filteredUsers.map((user) => (
                            <Paper key={user.id} withBorder radius="sm" p="xs">
                              <Group justify="space-between" wrap="nowrap">
                                <Text size="sm" truncate>
                                  {user.name}
                                </Text>

                                <Button
                                  variant="light"
                                  size="compact-sm"
                                  onClick={() => addUser(user)}
                                >
                                  {t('userGroupForm.addUser')}
                                </Button>
                              </Group>
                            </Paper>
                          ))}
                        </Stack>
                      </ScrollArea>
                    )}
                  </Stack>
                </Paper>
              </Grid.Col>

              <Grid.Col span={{ base: 12, md: 6 }}>
                <Paper withBorder radius="md" p="lg" h="100%">
                  <Stack gap="sm">
                    <Group justify="space-between">
                      <Text fw={600}>{t('userGroupForm.groupUsers')}</Text>
                      <Badge variant="light">{selectedUsers.length}</Badge>
                    </Group>

                    {selectedUsers.length === 0 ? (
                      <Text size="sm" c="dimmed">
                        {t('userGroupForm.noSelectedUsers')}
                      </Text>
                    ) : (
                      <ScrollArea h={420} type="auto">
                        <Stack gap={4}>
                          {selectedUsers.map((user) => (
                            <Paper key={user.id} withBorder radius="sm" p="xs">
                              <Group justify="space-between" wrap="nowrap">
                                <Text size="sm" truncate>
                                  {user.name}
                                </Text>

                                <Button
                                  variant="subtle"
                                  color="red"
                                  size="compact-sm"
                                  leftSection={<IconX size={14} />}
                                  onClick={() => removeUser(user.id)}
                                >
                                  {t('userGroupForm.removeUser')}
                                </Button>
                              </Group>
                            </Paper>
                          ))}
                        </Stack>
                      </ScrollArea>
                    )}
                  </Stack>
                </Paper>
              </Grid.Col>
            </Grid>
          </Grid.Col>
        </Grid>
      )}
    </Stack>
  );
}
