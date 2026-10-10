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
import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';

import { getErrorMessage } from '@/api/getErrorMessage';
import { useCreateUserGroup, useUpdateUserGroup, useUserGroup } from '@/api/hooks/userGroups';
import { useUsers } from '@/api/hooks/users';
import type { User } from '@/api/interfaces/User/Users';
import type { CreateUserGroupInput, UserGroupUser } from '@/api/interfaces/UserGroup/UserGroups';
import { PageHeader } from '@/components/PageHeader/PageHeader';
import { useActiveLocation } from '@/features/locations/LocationContext';
import { colors } from '@/data/common';

const USERS_LIMIT = 100;

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
  const [userSearch, setUserSearch] = useState('');

  const groupQuery = useUserGroup(isEditing ? groupId : undefined);
  const usersQuery = useUsers(
    {
      locationId: location?.id ?? '',
      name: '',
      isActive: true,
      page: 1,
      limit: USERS_LIMIT,
    },
    Boolean(location)
  );
  const createGroup = useCreateUserGroup(location?.id ?? '');
  const updateGroup = useUpdateUserGroup(location?.id ?? '');

  const availableUsers = usersQuery.data?.data ?? [];

  const hydratedGroupId = useRef<string | null>(null);

  useEffect(() => {
    const group = groupQuery.data;

    if (!group || hydratedGroupId.current === group.id) {
      return;
    }

    hydratedGroupId.current = group.id;
    setName(group.name);
    setColor(group.color);
    setSelectedUsers(group.userIds);
  }, [groupQuery.data]);

  const loading = usersQuery.isPending || (isEditing && groupQuery.isPending);

  const loadError = usersQuery.isError
    ? getErrorMessage(usersQuery.error, t('errors.unexpected'))
    : groupQuery.isError
      ? getErrorMessage(groupQuery.error, t('errors.unexpected'))
      : null;

  const mutationError = createGroup.error ?? updateGroup.error;
  const error = mutationError ? getErrorMessage(mutationError, t('errors.unexpected')) : loadError;

  const selectedIds = useMemo(() => new Set(selectedUsers.map((user) => user.id)), [selectedUsers]);

  const sortedSelectedUsers = useMemo(
    () => [...selectedUsers].sort((a, b) => a.name.localeCompare(b.name, 'es')),
    [selectedUsers]
  );

  const filteredUsers = useMemo(() => {
    const query = userSearch.trim().toLowerCase();

    return availableUsers
      .filter(
        (user) =>
          !selectedIds.has(user.id) &&
          (query.length === 0 || user.name.toLowerCase().includes(query))
      )
      .sort((a, b) => a.name.localeCompare(b.name, 'es'));
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

  function handleSave() {
    if (!location) {
      return;
    }

    const trimmedName = name.trim();

    if (trimmedName.length === 0) {
      setNameError(t('userGroupForm.nameRequired'));
      return;
    }

    setNameError(null);

    const payload: CreateUserGroupInput = {
      name: trimmedName,
      color,
      locationId: location.id,
      userIds: selectedUsers.map((user) => user.id),
    };

    if (isEditing && groupId) {
      updateGroup.mutate({ id: groupId, ...payload }, { onSuccess: goBack });
    } else {
      createGroup.mutate(payload, { onSuccess: goBack });
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
            <Button
              variant="default"
              onClick={goBack}
              disabled={createGroup.isPending || updateGroup.isPending}
            >
              {t('common.cancel')}
            </Button>

            <Button onClick={handleSave} loading={createGroup.isPending || updateGroup.isPending}>
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
                          {sortedSelectedUsers.map((user) => (
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
