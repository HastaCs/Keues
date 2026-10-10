import {
  ActionIcon,
  Alert,
  Box,
  Button,
  Center,
  Divider,
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
import {
  IconArrowRight,
  IconBuildingStore,
  IconEdit,
  IconSearch,
  IconTrash,
} from '@tabler/icons-react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { getErrorMessage } from '@/api/getErrorMessage';
import {
  useCreateLocation,
  useLocations,
  useRemoveLocation,
  useUpdateLocation,
} from '@/api/hooks/locations';
import {
  LocationInput,
  LocationKeue,
  UpdateLocationInput,
} from '@/api/interfaces/Location/Locations';
import { EmptyState } from '@/components/EmptyState/EmptyState';
import { PageHeader } from '@/components/PageHeader/PageHeader';
import cardHoverClasses from '@/styles/card-hover.module.css';
import { LocationFormModal } from './LocationFormModal';

function sortLocations(items: LocationKeue[], direction: string): LocationKeue[] {
  const sorted = [...items].sort((left, right) => left.name.localeCompare(right.name, 'es'));
  return direction === 'asc' ? sorted : sorted.reverse();
}

export function LocationListPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [sortDirection, setSortDirection] = useState<string>('asc');
  const [formOpened, setFormOpened] = useState(false);
  const [editingLocation, setEditingLocation] = useState<LocationKeue | undefined>(undefined);
  const [deletingLocation, setDeletingLocation] = useState<LocationKeue | undefined>(undefined);

  const locationsQuery = useLocations();
  const createLocation = useCreateLocation();
  const updateLocation = useUpdateLocation();
  const removeLocation = useRemoveLocation();

  const locations = locationsQuery.data ?? [];

  const loadError = locationsQuery.isError
    ? getErrorMessage(locationsQuery.error, t('errors.unexpected'))
    : null;
  const mutationError = createLocation.error ?? updateLocation.error ?? removeLocation.error;
  const error = mutationError ? getErrorMessage(mutationError, t('errors.unexpected')) : loadError;

  const filteredLocations = useMemo(() => {
    const query = search.trim().toLowerCase();
    const matches = query.length
      ? locations.filter((location) => {
          const normalizedName = location.name.toLowerCase();
          const normalizedDescription = location.description.toLowerCase();
          return normalizedName.includes(query) || normalizedDescription.includes(query);
        })
      : locations;

    return sortLocations(matches, sortDirection);
  }, [locations, search, sortDirection]);

  function openCreateModal() {
    setEditingLocation(undefined);
    setFormOpened(true);
  }

  function openEditModal(location: LocationKeue) {
    setEditingLocation(location);
    setFormOpened(true);
  }

  async function handleSubmitLocation(payload: LocationInput) {
    const closeForm = () => {
      setFormOpened(false);
      setEditingLocation(undefined);
    };

    if (editingLocation) {
      const input: UpdateLocationInput = {
        id: editingLocation.id,
        name: payload.name,
        description: payload.description,
        color: payload.color,
      };

      updateLocation.mutate(input, { onSuccess: closeForm });
    } else {
      createLocation.mutate(payload, { onSuccess: closeForm });
    }
  }

  function handleConfirmDeleteLocation() {
    if (!deletingLocation) {
      return;
    }

    removeLocation.mutate(deletingLocation.id, {
      onSuccess: () => setDeletingLocation(undefined),
    });
  }

  return (
    <>
      <Modal
        opened={Boolean(deletingLocation)}
        onClose={() => setDeletingLocation(undefined)}
        title={t('locations.deleteTitle')}
        centered
      >
        <Stack gap="lg">
          <Text>{t('locations.deleteDescription', { name: deletingLocation?.name ?? '' })}</Text>
          <Group justify="flex-end">
            <Button variant="default" onClick={() => setDeletingLocation(undefined)}>
              {t('common.cancel')}
            </Button>
            <Button
              color="red"
              leftSection={<IconTrash size={14} />}
              onClick={handleConfirmDeleteLocation}
            >
              {t('common.delete')}
            </Button>
          </Group>
        </Stack>
      </Modal>

      <LocationFormModal
        opened={formOpened}
        initialLocation={editingLocation}
        loading={createLocation.isPending || updateLocation.isPending}
        onClose={() => setFormOpened(false)}
        onSubmit={handleSubmitLocation}
      />

      <Stack gap="lg">
        <PageHeader
          title={t('locations.pickOne')}
          actions={
            locations.length > 0 ? (
              <Button onClick={openCreateModal}>{t('locations.newLocation')}</Button>
            ) : undefined
          }
        />

        {error ? (
          <Alert color="red" title={t('errors.requestFailed')}>
            {error}
          </Alert>
        ) : null}

        <Group align="flex-end" justify="space-between">
          <TextInput
            value={search}
            onChange={(event) => setSearch(event.currentTarget.value)}
            placeholder={t('locations.searchPlaceholder')}
            leftSection={<IconSearch size={16} />}
            style={{ maxWidth: 420, width: '100%' }}
          />

          <Select
            value={sortDirection}
            onChange={(value) => setSortDirection((value as string) ?? 'asc')}
            data={[
              { value: 'asc', label: t('locations.sortAZ') },
              { value: 'desc', label: t('locations.sortZA') },
            ]}
            allowDeselect={false}
            style={{ minWidth: 180 }}
          />
        </Group>

        {locationsQuery.isPending ? (
          <Center py={64}>
            <Loader />
          </Center>
        ) : filteredLocations.length === 0 ? (
          <EmptyState
            title={t('locations.emptyTitle')}
            description={t('locations.emptyDescription')}
            action={
              locations.length === 0 ? (
                <Button onClick={openCreateModal}>{t('locations.newLocation')}</Button>
              ) : undefined
            }
          />
        ) : (
          <SimpleGrid cols={{ base: 1, md: 2, xl: 3 }} spacing="md" verticalSpacing="md">
            {filteredLocations.map((location) => {
              return (
                <Paper
                  key={location.id}
                  withBorder
                  radius="lg"
                  shadow="sm"
                  className={cardHoverClasses.interactiveCard}
                  style={{
                    cursor: 'pointer',
                    overflow: 'hidden',
                  }}
                  onClick={() => navigate(`/locations/${location.id}`)}
                >
                  <Box h={6} bg={location.color} />

                  <Stack p="lg" gap="lg">
                    <Group justify="space-between" align="flex-start">
                      <Group wrap="nowrap">
                        <ThemeIcon size={60} radius="xl" color={location.color} variant="light">
                          <IconBuildingStore size={30} />
                        </ThemeIcon>

                        <Stack gap={2}>
                          <Text fw={700} size="lg">
                            {location.name}
                          </Text>

                          <Text size="sm" c="dimmed" lineClamp={2}>
                            {location.description || t('counters.noDescription')}
                          </Text>
                        </Stack>
                      </Group>

                      <Group gap={4}>
                        <Tooltip label={t('common.edit')}>
                          <ActionIcon
                            variant="light"
                            color="blue"
                            onClick={(e) => {
                              e.stopPropagation();
                              openEditModal(location);
                            }}
                          >
                            <IconEdit size={16} />
                          </ActionIcon>
                        </Tooltip>

                        <Tooltip label={t('common.delete')}>
                          <ActionIcon
                            variant="light"
                            color="red"
                            onClick={(e) => {
                              e.stopPropagation();
                              setDeletingLocation(location);
                            }}
                          >
                            <IconTrash size={16} />
                          </ActionIcon>
                        </Tooltip>
                      </Group>
                    </Group>

                    <Divider />

                    <Group justify="space-between">
                      <Text size="sm" c="dimmed">
                        {t('locations.enterLocation')}
                      </Text>

                      <ThemeIcon variant="light" color={location.color} radius="xl">
                        <IconArrowRight size={18} />
                      </ThemeIcon>
                    </Group>
                  </Stack>
                </Paper>
              );
            })}
          </SimpleGrid>
        )}
      </Stack>
    </>
  );
}
