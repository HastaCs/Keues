import { ActionIcon, Avatar, Group, Menu, Text } from '@mantine/core';
import { IconLogout } from '@tabler/icons-react';
import { useTranslation } from 'react-i18next';
import { UserRole } from '@/api/interfaces/User/Users';
import { useAuth } from '@/auth/AuthContext';

export function UserMenu() {
  const { t } = useTranslation();
  const { user, logout } = useAuth();

  const initial = user?.name?.trim()?.charAt(0)?.toUpperCase() ?? '?';

  return (
    <Menu position="bottom-end" width={240} withArrow>
      <Menu.Target>
        <ActionIcon variant="default" size="lg" radius="md" aria-label={user?.name ?? t('user.name')}>
          <Avatar size={24} radius="xl" color="blue" src={undefined}>
            {initial}
          </Avatar>
        </ActionIcon>
      </Menu.Target>

      <Menu.Dropdown>
        <Group px="sm" py="xs" gap={3}>
          <Text size="sm" fw={600} truncate>
            {user?.name || user?.email || t('user.name')}
          </Text>
          <Text size="xs" c="dimmed">
            {user?.role === UserRole.Admin ? t('user.roleAdmin') : t('user.roleUser')}
          </Text>
        </Group>

        <Menu.Divider />

        <Menu.Item color="red" leftSection={<IconLogout size={16} />} onClick={() => void logout()}>
          {t('user.logout')}
        </Menu.Item>
      </Menu.Dropdown>
    </Menu>
  );
}