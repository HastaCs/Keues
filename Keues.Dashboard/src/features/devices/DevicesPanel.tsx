import { useEffect, useState } from 'react';
import { devicesApi } from '@/api/DevicesApi';

import {
  Alert,
  Badge,
  Button,
  Card,
  Center,
  Divider,
  Group,
  Indicator,
  Loader,
  Modal,
  Paper,
  SimpleGrid,
  Stack,
  Text,
  ThemeIcon,
} from '@mantine/core';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/hover-card.module.css';

import { IconClock, IconTrash, IconWifi } from '@tabler/icons-react';

import { useActiveLocation } from '@/features/locations/LocationContext';
import { PageHeader } from '@/components/PageHeader/PageHeader';

import type { Device } from '@/api/interfaces/Devices/Devices';

interface DevicesPanelProps {
  ns: string;
  deviceType: number;
  icon: React.ComponentType<{ size?: number; stroke?: number }>;
}

function formatLastConnection(
  value: string | null,
  ns: string,
  t: (key: string, options?: { count?: number }) => string
): string {
  if (!value) {
    return t(`${ns}.never`);
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return t(`${ns}.never`);
  }

  const seconds = Math.floor((Date.now() - date.getTime()) / 1000);

  if (seconds < 60) {
    return t(`${ns}.justNow`);
  }

  const minutes = Math.floor(seconds / 60);

  if (minutes < 60) {
    return t(`${ns}.minutes`, { count: minutes });
  }

  const hours = Math.floor(minutes / 60);

  if (hours < 24) {
    return t(`${ns}.hours`, { count: hours });
  }

  const days = Math.floor(hours / 24);

  if (days < 30) {
    return t(`${ns}.days`, { count: days });
  }

  return date.toLocaleDateString();
}

export function DevicesPanel({ ns, deviceType, icon: DeviceIcon }: DevicesPanelProps) {
  const { t } = useTranslation();
  const location = useActiveLocation();
  const [machines, setMachines] = useState<Device[]>([]);

  const [loading, setLoading] = useState(true);

  const [error, setError] = useState<string | null>(null);

  const [deletingDevice, setDeletingDevice] = useState<Device | null>(null);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    if (!location) {
      setMachines([]);
      setLoading(false);
      return;
    }

    void cargarDevices();
  }, [location]);

  async function cargarDevices() {
    if (!location) {
      return;
    }

    try {
      setLoading(true);
      setError(null);

      let response;

      if (deviceType === 0) {
        response = await devicesApi.listMachines(location.id);
      } else if (deviceType === 1) {
        response = await devicesApi.listCounters(location.id);
      } else {
        response = await devicesApi.listMonitors(location.id);
      }

      setMachines(response.data ?? []);
    } catch (error: any) {
      setError(error.message ?? t(`${ns}.loadError`));
    } finally {
      setLoading(false);
    }
  }

  async function handleConfirmDelete() {
    if (!deletingDevice) {
      return;
    }

    try {
      setDeleting(true);
      await devicesApi.remove(deletingDevice.id);
      setDeletingDevice(null);
      await cargarDevices();
    } catch (requestError: any) {
      setError(requestError.message ?? t('errors.unexpected'));
    } finally {
      setDeleting(false);
    }
  }

  if (!location) {
    return null;
  }

  const connectedMachines = machines.filter((machine) => machine.isConnected).length;

  return (
    <Stack gap="xl">
      <Modal
        opened={Boolean(deletingDevice)}
        onClose={() => setDeletingDevice(null)}
        title={t('devices.deleteTitle')}
        centered
      >
        <Stack gap="lg">
          <Text>
            {t('devices.deleteDescription', {
              name: deletingDevice?.name ?? '',
            })}
          </Text>

          <Group justify="flex-end">
            <Button variant="default" onClick={() => setDeletingDevice(null)} disabled={deleting}>
              {t('common.cancel')}
            </Button>

            <Button color="red" onClick={handleConfirmDelete} loading={deleting}>
              {t('common.delete')}
            </Button>
          </Group>
        </Stack>
      </Modal>

      <PageHeader
        label={t(`${ns}.title`)}
        title={location.name}
        description={t(`${ns}.subtitle`)}
        actions={
          !loading && !error ? (
            <Group gap="sm">
              <Badge size="lg" variant="light">
                {t(`${ns}.device`, { count: machines.length })}
              </Badge>

              <Badge size="lg" color="green" variant="light" leftSection={<IconWifi size={14} />}>
                {t(`${ns}.connected`, { count: connectedMachines })}
              </Badge>
            </Group>
          ) : undefined
        }
      />

      {/* Loading */}
      {loading && (
        <Center py="xl">
          <Loader />
        </Center>
      )}

      {/* Error */}
      {!loading && error && (
        <Alert color="red" title={t('errors.requestFailed')}>
          {error}
        </Alert>
      )}

      {/* Sin dispositivos */}
      {!loading && !error && machines.length === 0 && (
        <Paper withBorder radius="md" p="xl">
          <Stack align="center" gap={6}>
            <Text fw={600}>{t(`${ns}.emptyTitle`)}</Text>
            <Text c="dimmed" ta="center">
              {t(`${ns}.emptyDescription`)}
            </Text>
          </Stack>
        </Paper>
      )}

      {/* Dispositivos */}
      {!loading && !error && machines.length > 0 && (
        <SimpleGrid
          cols={{
            base: 1,
            sm: 2,
            md: 3,
            lg: 4,
          }}
          spacing="lg"
        >
          {machines.map((machine) => (
            <Card key={machine.id} withBorder radius="lg" padding="lg" className={styles.hoverCard}>
              <Stack gap="md">
                {/* Cabecera de la card */}
                <Group justify="space-between" align="flex-start">
                  <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
                    <Indicator
                      position="bottom-end"
                      offset={4}
                      size={11}
                      color={machine.isConnected ? 'green' : 'gray'}
                      processing={machine.isConnected}
                    >
                      <ThemeIcon
                        size={44}
                        radius="xl"
                        color={machine.isConnected ? 'blue' : 'gray'}
                        variant="light"
                      >
                        <DeviceIcon size={22} />
                      </ThemeIcon>
                    </Indicator>

                    <Stack gap={2} style={{ minWidth: 0 }}>
                      <Text fw={700} truncate>
                        {machine.name}
                      </Text>

                      <Text size="xs" c="dimmed">
                        {t(`${ns}.deviceType`)}
                      </Text>
                    </Stack>
                  </Group>

                  <Text
                    size="xs"
                    fw={600}
                    c={machine.isConnected ? 'green' : 'gray'}
                    style={{
                      whiteSpace: 'nowrap',
                    }}
                  >
                    {machine.isConnected
                      ? t(`${ns}.connectedStatus`)
                      : t(`${ns}.disconnectedStatus`)}
                  </Text>
                </Group>

                <Divider />

                {/* Última conexión */}
                <Group gap="xs" wrap="nowrap" align="center">
                  <IconClock
                    size={17}
                    stroke={1.5}
                    style={{
                      flexShrink: 0,
                    }}
                  />

                  <Stack gap={0}>
                    <Text size="xs" c="dimmed">
                      {t(`${ns}.lastConnection`)}
                    </Text>

                    <Text size="sm">{formatLastConnection(machine.lastConnection, ns, t)}</Text>
                  </Stack>
                </Group>

                {/* ID */}
                <Stack gap={2}>
                  <Text size="xs" c="dimmed">
                    {t(`${ns}.deviceId`)}
                  </Text>

                  <Text size="xs" ff="monospace" c="dimmed" truncate>
                    {machine.id}
                  </Text>
                </Stack>

                {!machine.isConnected && (
                  <Button
                    variant="light"
                    color="red"
                    size="xs"
                    leftSection={<IconTrash size={14} />}
                    onClick={() => setDeletingDevice(machine)}
                  >
                    {t('common.delete')}
                  </Button>
                )}
              </Stack>
            </Card>
          ))}
        </SimpleGrid>
      )}
    </Stack>
  );
}
