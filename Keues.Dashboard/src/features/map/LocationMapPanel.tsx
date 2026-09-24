import '@xyflow/react/dist/style.css';
import {
  Alert,
  Button,
  Group,
  Loader,
  MultiSelect,
  Paper,
  Stack,
  Tabs,
  Text,
  Tooltip,
  useMantineColorScheme,
} from '@mantine/core';
import {
  Background,
  Controls,
  MarkerType,
  MiniMap,
  ReactFlow,
  useEdgesState,
  useNodesState,
  useReactFlow,
  type Edge,
  type ReactFlowInstance,
} from '@xyflow/react';
import { IconAlertTriangle, IconArmchair, IconGitBranch, IconRefresh } from '@tabler/icons-react';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { countersApi } from '@/api/CountersApi';
import { flowsApi } from '@/api/FlowsApi';
import { ApiError } from '@/api/httpClient';
import { queuesApi } from '@/api/QueuesApi';
import { userGroupsApi } from '@/api/UserGroupsApi';
import { usersApi } from '@/api/UsersApi';
import type { User } from '@/api/interfaces/User/Users';
import { EmptyState } from '@/components/EmptyState/EmptyState';
import { PageHeader } from '@/components/PageHeader/PageHeader';
import { useActiveLocation } from '@/features/locations/LocationContext';
import { buildCounterGraph, buildFlowGraph, type MapSourceData } from './graphBuilder';
import { IssuesDrawer } from './IssuesDrawer';
import { MapNode } from './MapNode';
import type { MapGraph, MapGraphNode, MapView } from './types';

const nodeTypes = { mapNode: MapNode };

const USERS_PAGE_LIMIT = 100;
const USERS_MAX_PAGES = 100;

async function fetchAllUsers(locationId: string): Promise<User[]> {
  const all: User[] = [];
  let page = 1;

  for (;;) {
    const response = await usersApi.list({
      locationId,
      page,
      limit: USERS_PAGE_LIMIT,
      sortOrder: 'asc',
    });

    all.push(...response.data);

    const totalPages = response.pagination?.totalPages ?? 1;
    if (response.data.length < USERS_PAGE_LIMIT || page >= totalPages || page >= USERS_MAX_PAGES) {
      break;
    }

    page += 1;
  }

  return all;
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

function FitViewOnChange({ trigger }: { trigger: unknown }) {
  const { fitView } = useReactFlow();

  useEffect(() => {
    const handle = window.setTimeout(() => {
      void fitView({ duration: 400, padding: 0.2 });
    }, 60);

    return () => window.clearTimeout(handle);
  }, [trigger, fitView]);

  return null;
}

function MapCanvas({
  graph,
  colorMode,
  onInit,
}: {
  graph: MapGraph;
  colorMode: 'light' | 'dark';
  onInit: (instance: ReactFlowInstance<MapGraphNode, Edge>) => void;
}) {
  const [nodes, setNodes, onNodesChange] = useNodesState<MapGraphNode>(graph.nodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>(graph.edges);

  useEffect(() => {
    setNodes(graph.nodes);
    setEdges(graph.edges);
  }, [graph, setNodes, setEdges]);

  return (
    <ReactFlow
      nodes={nodes}
      edges={edges}
      onNodesChange={onNodesChange}
      onEdgesChange={onEdgesChange}
      nodeTypes={nodeTypes}
      colorMode={colorMode}
      defaultEdgeOptions={{ markerEnd: { type: MarkerType.ArrowClosed } }}
      nodesDraggable
      nodesConnectable={false}
      fitView
      minZoom={0.2}
      maxZoom={2}
      onInit={onInit}
    >
      <Background />
      <Controls />
      <MiniMap pannable zoomable />
      <FitViewOnChange trigger={graph} />
    </ReactFlow>
  );
}

export function LocationMapPanel() {
  const { t } = useTranslation();
  const location = useActiveLocation();
  const { colorScheme } = useMantineColorScheme();

  const [view, setView] = useState<MapView>('machines');
  const [source, setSource] = useState<MapSourceData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [issuesOpened, setIssuesOpened] = useState(false);
  const [selectedCounters, setSelectedCounters] = useState<string[] | null>(null);
  const [selectedFlows, setSelectedFlows] = useState<string[] | null>(null);
  const [instance, setInstance] = useState<ReactFlowInstance<MapGraphNode, Edge> | null>(null);

  const allCounterIds = useMemo(
    () => source?.counters.map((counter) => counter.id) ?? [],
    [source]
  );
  const activeCounterIds = selectedCounters ?? allCounterIds;

  const counterOptions = useMemo(
    () =>
      (source?.counters ?? []).map((counter) => ({
        value: counter.id,
        label: `${counter.code} · ${counter.name}`,
      })),
    [source]
  );

  const allFlowIds = useMemo(() => source?.flows.map((flow) => flow.id) ?? [], [source]);
  const activeFlowIds = selectedFlows ?? allFlowIds;

  const flowOptions = useMemo(
    () => (source?.flows ?? []).map((flow) => ({ value: flow.id, label: flow.name })),
    [source]
  );

  const load = useCallback(async () => {
    if (!location) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const [queues, counters, groups, flows, users] = await Promise.all([
        queuesApi.list(location.id),
        countersApi.list(location.id),
        userGroupsApi.list(location.id),
        flowsApi.list(location.id),
        fetchAllUsers(location.id),
      ]);

      setSource({
        queues: queues.data,
        counters: counters.data,
        groups: groups.data,
        flows: flows.data,
        users,
      });
    } catch (requestError) {
      setError(getErrorMessage(requestError, t('errors.unexpected')));
    } finally {
      setLoading(false);
    }
  }, [location, t]);

  useEffect(() => {
    void load();
  }, [load]);

  const graph = useMemo(() => {
    if (!source) {
      return null;
    }

    return view === 'machines'
      ? buildFlowGraph(source, activeFlowIds)
      : buildCounterGraph(source, activeCounterIds);
  }, [source, view, activeCounterIds, activeFlowIds]);

  function focusNode(nodeId: string) {
    void instance?.fitView({
      nodes: [{ id: nodeId }],
      duration: 600,
      padding: 0.5,
      maxZoom: 1.6,
    });
  }

  if (!location) {
    return null;
  }

  return (
    <Stack gap="md">
      <PageHeader
        label={location.name}
        title={t('map.heading')}
        description={t(`map.subtitle.${view}`)}
        actions={
          <Group gap="sm">
            <Button
              variant="default"
              leftSection={<IconRefresh size={16} />}
              loading={loading}
              onClick={() => void load()}
            >
              {t('map.refresh')}
            </Button>

            <Tooltip label={t('map.issuesHint')} withArrow multiline w={260}>
              <Button
                color={graph && graph.issues.length > 0 ? 'red' : 'gray'}
                variant={graph && graph.issues.length > 0 ? 'light' : 'default'}
                leftSection={<IconAlertTriangle size={16} />}
                onClick={() => setIssuesOpened(true)}
              >
                {t('map.issuesButton')}
                {graph ? ` (${graph.issues.length})` : ''}
              </Button>
            </Tooltip>
          </Group>
        }
      />

      <Tabs value={view} onChange={(value) => setView(value as MapView)}>
        <Tabs.List>
          <Tabs.Tab value="machines" leftSection={<IconGitBranch size={15} />}>
            {t('map.tabs.machines')}
          </Tabs.Tab>
          <Tabs.Tab value="counters" leftSection={<IconArmchair size={15} />}>
            {t('map.tabs.counters')}
          </Tabs.Tab>
        </Tabs.List>
      </Tabs>

      {error ? <Alert color="red">{error}</Alert> : null}

      {loading && !graph ? (
        <Group justify="center" py="xl">
          <Loader />
        </Group>
      ) : graph ? (
        <>
          <Group align="stretch" gap="md" wrap="wrap">
            <Stack gap="sm" style={{ flex: '1 1 520px', minWidth: 0 }}>
              <Paper
                withBorder
                radius="md"
                style={{ height: 'calc(100vh - 220px)', minHeight: 520 }}
              >
                {graph.nodes.length === 0 ? (
                  <Group justify="center" align="center" h="100%">
                    <EmptyState
                      title={t('map.emptyTitle')}
                      description={t('map.emptyDescription')}
                    />
                  </Group>
                ) : (
                  <MapCanvas
                    key={view}
                    graph={graph}
                    colorMode={colorScheme === 'dark' ? 'dark' : 'light'}
                    onInit={setInstance}
                  />
                )}
              </Paper>
            </Stack>

            {source ? (
              <Paper withBorder radius="md" p="md" style={{ flex: '0 0 300px' }}>
                <Stack gap="sm">
                  <Text fw={700} size="sm">
                    {view === 'machines' ? t('map.filterFlows') : t('map.filterCounters')}
                  </Text>

                  {view === 'machines' ? (
                    <MultiSelect
                      placeholder={t('map.filterFlowsPlaceholder')}
                      data={flowOptions}
                      value={activeFlowIds}
                      onChange={setSelectedFlows}
                      searchable
                      clearable
                    />
                  ) : (
                    <MultiSelect
                      placeholder={t('map.filterCountersPlaceholder')}
                      data={counterOptions}
                      value={activeCounterIds}
                      onChange={setSelectedCounters}
                      searchable
                      clearable
                    />
                  )}

                  <Group gap="xs" grow>
                    <Button
                      size="xs"
                      variant="default"
                      onClick={() =>
                        view === 'machines'
                          ? setSelectedFlows(allFlowIds)
                          : setSelectedCounters(allCounterIds)
                      }
                    >
                      {t('map.selectAll')}
                    </Button>

                    <Button
                      size="xs"
                      variant="default"
                      onClick={() =>
                        view === 'machines' ? setSelectedFlows([]) : setSelectedCounters([])
                      }
                    >
                      {t('map.selectNone')}
                    </Button>
                  </Group>
                </Stack>
              </Paper>
            ) : null}
          </Group>

          <IssuesDrawer
            opened={issuesOpened}
            issues={graph.issues}
            onClose={() => setIssuesOpened(false)}
            onSelect={focusNode}
          />
        </>
      ) : null}
    </Stack>
  );
}
