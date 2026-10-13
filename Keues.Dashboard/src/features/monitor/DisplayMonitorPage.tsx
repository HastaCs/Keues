import { Alert, Box, Group, Select, Stack } from '@mantine/core';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { getErrorMessage } from '@/api/getErrorMessage';
import { useFlows } from '@/api/hooks/flows';
import { useLocations } from '@/api/hooks/locations';
import { MonitorScreen } from './MonitorScreen';
import { primeAudio } from './sound';
import { useMonitorConnection } from './useMonitorConnection';

const SELECTION_STORAGE_KEY = 'keues-display-monitor:selection';

const MANUAL_CALL_FLOW_TYPE = 2;

interface StoredSelection {
  locationId: string | null;
  flowId: string | null;
}

function emptySelection(): StoredSelection {
  return { locationId: null, flowId: null };
}

function getStoredSelection(): StoredSelection {
  const raw = window.localStorage.getItem(SELECTION_STORAGE_KEY);
  if (!raw) {
    return emptySelection();
  }

  try {
    const parsed = JSON.parse(raw) as Partial<StoredSelection>;
    return { locationId: parsed.locationId ?? null, flowId: parsed.flowId ?? null };
  } catch {
    return emptySelection();
  }
}

export function DisplayMonitorPage() {
  const { t } = useTranslation();
  const [initialSelection] = useState(getStoredSelection);
  const [locationId, setLocationId] = useState<string | null>(initialSelection.locationId);
  const [flowId, setFlowId] = useState<string | null>(initialSelection.flowId);

  const locationsQuery = useLocations();
  const flowsQuery = useFlows(locationId ?? undefined);

  const locations = locationsQuery.data ?? [];
  const flows = useMemo(
    () => (flowsQuery.data ?? []).filter((flow) => flow.flowType === MANUAL_CALL_FLOW_TYPE),
    [flowsQuery.data]
  );

  const { status, turns } = useMonitorConnection(locationId ?? '', flowId ?? '');

  useEffect(() => {
    window.localStorage.setItem(SELECTION_STORAGE_KEY, JSON.stringify({ locationId, flowId }));
  }, [locationId, flowId]);

  useEffect(() => {
    const unlock = () => primeAudio();
    window.addEventListener('pointerdown', unlock, { once: true });
    return () => window.removeEventListener('pointerdown', unlock);
  }, []);

  useEffect(() => {
    if (locationId && locationsQuery.isSuccess && !locations.some((l) => l.id === locationId)) {
      setLocationId(null);
      setFlowId(null);
    }
  }, [locationId, locationsQuery.isSuccess, locations]);

  useEffect(() => {
    if (flowId && flowsQuery.isSuccess && !flows.some((f) => f.id === flowId)) {
      setFlowId(null);
    }
  }, [flowId, flowsQuery.isSuccess, flows]);

  if (locationsQuery.isError) {
    return (
      <Box p="md">
        <Alert color="red">{getErrorMessage(locationsQuery.error, t('errors.unexpected'))}</Alert>
      </Box>
    );
  }

  const ready = Boolean(locationId && flowId);

  return (
    <Box p="md">
      <Stack gap="md">
        <Group grow>
          <Select
            placeholder={t('flows.displayMonitorLocationPlaceholder')}
            data={locations.map((location) => ({ value: location.id, label: location.name }))}
            value={locationId}
            onChange={(value) => {
              primeAudio();
              setLocationId(value);
              setFlowId(null);
            }}
            searchable
            allowDeselect={false}
          />

          <Select
            placeholder={t('flows.displayMonitorFlowPlaceholder')}
            data={flows.map((flow) => ({ value: flow.id, label: flow.name }))}
            value={flowId}
            onChange={(value) => {
              primeAudio();
              setFlowId(value);
            }}
            disabled={!locationId}
            searchable
            allowDeselect={false}
          />
        </Group>

        {ready ? (
          <MonitorScreen status={status} turns={turns} />
        ) : (
          <Alert color="blue">{t('flows.displayMonitorSelectPrompt')}</Alert>
        )}
      </Stack>
    </Box>
  );
}
