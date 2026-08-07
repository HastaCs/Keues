import {
  ActionIcon,
  Anchor,
  AppShell,
  Badge,
  Breadcrumbs,
  Burger,
  Button,
  Center,
  Group,
  Loader,
  NavLink,
  Stack,
  Text,
  useMantineColorScheme,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import {
  IconCategory,
  IconChevronRight,
  IconDeviceTabletDown,
  IconDeviceTv,
  IconGitBranch,
  IconLayoutDashboard,
  IconMapPin,
  IconMoon,
  IconPlugConnected,
  IconSettings,
  IconSun,
  IconTicket,
  IconUsers,
} from '@tabler/icons-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, Navigate, Outlet, useLocation } from 'react-router-dom';

import { locationsApi } from '@/api/LocationsApi';
import type { LocationKeue } from '@/api/interfaces/Location/Locations';
import logoHorizontal from '@/assets/logos/horizontal.png';
import { SettingsModal } from '@/components/SettingsModal/SettingsModal';
import { UserMenu } from '@/components/UserMenu/UserMenu';
import { LocationContext } from '@/features/locations/LocationContext';
import { APP_VERSION } from '@/version';

type WorkspaceModule =
  | 'dashboard'
  | 'tickets'
  | 'counters'
  | 'ticketTypes'
  | 'flows'
  | 'devices'
  | 'deviceCounters'
  | 'deviceTicketMachines'
  | 'deviceMonitors';

interface NavigationItem {
  id: WorkspaceModule;
  label: string;
  icon: React.ComponentType<{ size?: number; stroke?: number }>;
  children?: NavigationItem[];
}

interface LocationState {
  locationId?: string;
  location: LocationKeue | null;
  failed: boolean;
}

const navigationItems: NavigationItem[] = [
  { id: 'dashboard', label: 'sidebar.dashboard', icon: IconLayoutDashboard },
  { id: 'tickets', label: 'sidebar.tickets', icon: IconTicket },
  { id: 'counters', label: 'sidebar.counters', icon: IconUsers },
  { id: 'ticketTypes', label: 'sidebar.ticketTypes', icon: IconCategory },
  { id: 'flows', label: 'sidebar.flows', icon: IconGitBranch },

  {
    id: 'devices',
    label: 'sidebar.devices',
    icon: IconPlugConnected,
    children: [
      { id: 'deviceCounters', label: 'sidebar.deviceCounters', icon: IconUsers },
      {
        id: 'deviceTicketMachines',
        label: 'sidebar.deviceTicketMachines',
        icon: IconDeviceTabletDown,
      },
      { id: 'deviceMonitors', label: 'sidebar.deviceMonitors', icon: IconDeviceTv },
    ],
  },
];

export function AppShellLayout() {
  const { t } = useTranslation();
  const { colorScheme, setColorScheme } = useMantineColorScheme();
  const { pathname } = useLocation();

  const segments = pathname.split('/').filter(Boolean);
  const locationId = segments[0] === 'locations' ? segments[1] : undefined;
  const moduleSegment = segments[0] === 'locations' && segments[1] ? segments[2] : undefined;

  const [mobileOpened, { toggle: toggleMobile }] = useDisclosure();
  const [desktopOpened] = useDisclosure(true);
  const [settingsOpened, setSettingsOpened] = useState(false);
  const [locationState, setLocationState] = useState<LocationState>({
    locationId: undefined,
    location: null,
    failed: false,
  });

  useEffect(() => {
    if (!locationId) {
      setLocationState({ locationId: undefined, location: null, failed: false });
      return;
    }

    let cancelled = false;
    setLocationState({ locationId, location: null, failed: false });

    locationsApi
      .get(locationId)
      .then((response) => {
        if (!cancelled) {
          setLocationState({ locationId, location: response, failed: false });
        }
      })
      .catch(() => {
        if (!cancelled) {
          setLocationState({ locationId, location: null, failed: true });
        }
      });

    return () => {
      cancelled = true;
    };
  }, [locationId]);

  const location = locationState.locationId === locationId ? locationState.location : null;
  const loadingLocation =
    Boolean(locationId) && (locationState.locationId !== locationId || !location);
  const locationFailed =
    Boolean(locationId) && locationState.locationId === locationId && locationState.failed;

  const moduleLabelKey = locationId && moduleSegment ? `sidebar.${moduleSegment}` : undefined;

  const breadcrumbItems = [];
  if (locationId) {
    breadcrumbItems.push(
      <Anchor
        key="locations"
        component={Link}
        to="/locations"
        c="dimmed"
        underline="hover"
        size="sm"
      >
        {t('sidebar.locations')}
      </Anchor>
    );

    if (location) {
      if (moduleLabelKey) {
        breadcrumbItems.push(
          <Anchor
            key="location"
            component={Link}
            to={`/locations/${location.id}`}
            c="dimmed"
            underline="hover"
            size="sm"
          >
            {location.name}
          </Anchor>
        );

        breadcrumbItems.push(
          <Text key="module" c="dimmed" fw={600} size="sm">
            {t(moduleLabelKey)}
          </Text>
        );
      } else {
        breadcrumbItems.push(
          <Text key="location" c="dimmed" fw={600} size="sm">
            {location.name}
          </Text>
        );
      }
    }
  } else {
    breadcrumbItems.push(
      <Text key="locations" c="dimmed" fw={600} size="sm">
        {t('sidebar.locations')}
      </Text>
    );
  }

  const modulePath = (item: NavigationItem) =>
    item.id === 'dashboard' ? `/locations/${locationId}` : `/locations/${locationId}/${item.id}`;

  const isActive = (item: NavigationItem) => {
    if (item.children) {
      return item.children.some((child) => pathname === modulePath(child));
    }

    return pathname === modulePath(item);
  };

  return (
    <AppShell
      header={{ height: 60 }}
      navbar={{
        width: 248,
        breakpoint: 'sm',
        collapsed: { mobile: !mobileOpened, desktop: !desktopOpened },
      }}
      padding="lg"
      styles={{
        main: {
          backgroundColor: 'var(--mantine-color-body)',
        },
        navbar: {
          borderRight: '1px solid var(--mantine-color-default-border)',
          backgroundColor: 'var(--mantine-color-body)',
        },
        header: {
          borderBottom: '1px solid var(--mantine-color-default-border)',
          backgroundColor: 'var(--mantine-color-body)',
        },
      }}
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          <Group gap="md">
            <Burger
              opened={mobileOpened}
              onClick={toggleMobile}
              hiddenFrom="sm"
              size="sm"
              aria-label={t('sidebar.brand')}
            />

            <Breadcrumbs separator={<IconChevronRight size={14} />} separatorMargin={2}>
              {breadcrumbItems}
            </Breadcrumbs>
          </Group>

          <Group gap="sm">
            <ActionIcon
              variant="default"
              size="lg"
              radius="md"
              aria-label={t('common.theme')}
              onClick={() => setColorScheme(colorScheme === 'dark' ? 'light' : 'dark')}
            >
              {colorScheme === 'dark' ? <IconSun size={18} /> : <IconMoon size={18} />}
            </ActionIcon>

            <Button
              variant="default"
              leftSection={<IconSettings size={16} />}
              onClick={() => setSettingsOpened(true)}
            >
              {t('sidebar.settings')}
            </Button>

            <UserMenu />
          </Group>
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="md">
        <Stack h="100%" justify="space-between">
          <Stack gap="lg">
            <Group px="xs" justify="space-between" wrap="nowrap">
              <img
                src={logoHorizontal}
                alt={t('sidebar.brand')}
                style={{ height: 26, width: 'auto', objectFit: 'contain' }}
              />
            </Group>

            <Stack gap={6}>
              <NavLink
                component={Link}
                to="/locations"
                label={t('sidebar.locations')}
                active={!locationId}
                leftSection={<IconMapPin size={18} stroke={2.2} />}
                variant="light"
              />

              {location
                ? navigationItems.map((item) => {
                    const NavigationIcon = item.icon;

                    if (item.children) {
                      return (
                        <NavLink
                          key={item.id}
                          label={t(item.label)}
                          active={isActive(item)}
                          defaultOpened
                          leftSection={<NavigationIcon size={18} stroke={2.2} />}
                          variant="light"
                          childrenOffset={20}
                        >
                          {item.children.map((child) => {
                            const ChildIcon = child.icon;

                            return (
                              <NavLink
                                key={child.id}
                                component={Link}
                                to={modulePath(child)}
                                label={t(child.label)}
                                active={pathname === modulePath(child)}
                                leftSection={<ChildIcon size={16} stroke={2.2} />}
                                variant="light"
                              />
                            );
                          })}
                        </NavLink>
                      );
                    }

                    return (
                      <NavLink
                        key={item.id}
                        component={Link}
                        to={modulePath(item)}
                        label={t(item.label)}
                        active={pathname === modulePath(item)}
                        leftSection={<NavigationIcon size={18} stroke={2.2} />}
                        variant="light"
                      />
                    );
                  })
                : null}
            </Stack>
          </Stack>

          <Badge variant="outline" color="blue" size="sm" radius="sm" >
            v{APP_VERSION}
          </Badge>
        </Stack>
      </AppShell.Navbar>

      <AppShell.Main>
        {locationFailed ? (
          <Navigate to="/locations" replace />
        ) : loadingLocation ? (
          <Center py={80}>
            <Loader />
          </Center>
        ) : (
          <LocationContext.Provider value={location}>
            <Outlet />
          </LocationContext.Provider>
        )}
      </AppShell.Main>

      <SettingsModal opened={settingsOpened} onClose={() => setSettingsOpened(false)} />
    </AppShell>
  );
}
