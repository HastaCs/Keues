import '@xyflow/react/dist/style.css';
import {
  Alert,
  Badge,
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
import { MapLegend } from './MapLegend';
import { MapNode } from './MapNode';
import type { MapGraph, MapGraphNode, MapNodeKind, MapView } from './types';

const nodeTypes = { mapNode: MapNode };

const USERS_PAGE_LIMIT = 100;
const USERS_MAX_PAGES = 100;

const BADGE_COLOR: Record<MapNodeKind, string> = {
  flow: 'cyan',
  menu: 'orange',
  ticket: 'blue',
  queue: 'blue',
  counter: 'teal',
  group: 'grape',
  user: 'indigo',
};

const VIEW_STATS: Record<MapView, MapNodeKind[]> = {
  machines: ['flow', 'menu', 'ticket'],
  counters: ['counter', 'queue', 'group', 'user'],
};

function statValue(graph: MapGraph, kind: MapNodeKind): number {
  switch (kind) {
    case 'flow':
      return graph.stats.flows;
    case 'menu':
      return graph.stats.menus;
    case 'ticket':
      return graph.stats.tickets;
    case 'queue':
      return graph.stats.queues;
    case 'counter':
      return graph.stats.counters;
    case 'group':
      return graph.stats.groups;
    case 'user':
      return graph.stats.users;
    default:
      return 0;
  }
}

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
      ? buildFlowGraph(source)
      : buildCounterGraph(source, activeCounterIds);
  }, [source, view, activeCounterIds]);

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
    <Stack gap="lg">
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

      {view === 'counters' && source ? (
        <Group align="flex-end" gap="sm" wrap="wrap">
          <MultiSelect
            label={t('map.filterCounters')}
            placeholder={t('map.filterCountersPlaceholder')}
            data={counterOptions}
            value={activeCounterIds}
            onChange={setSelectedCounters}
            searchable
            clearable
            style={{ minWidth: 300, maxWidth: 520, flex: 1 }}
          />

          <Button variant="default" onClick={() => setSelectedCounters(allCounterIds)}>
            {t('map.selectAll')}
          </Button>

          <Button variant="default" onClick={() => setSelectedCounters([])}>
            {t('map.selectNone')}
          </Button>
        </Group>
      ) : null}

      {error ? <Alert color="red">{error}</Alert> : null}

      {loading && !graph ? (
        <Group justify="center" py="xl">
          <Loader />
        </Group>
      ) : graph && graph.nodes.length === 0 ? (
        <EmptyState title={t('map.emptyTitle')} description={t('map.emptyDescription')} />
      ) : graph ? (
        <>
          <Group justify="space-between" align="center" wrap="wrap" gap="md">
            <Group gap="xs" wrap="wrap">
              {VIEW_STATS[view].map((kind) => (
                <Badge key={kind} variant="light" color={BADGE_COLOR[kind]}>
                  {t(`map.kinds.${kind}`)}: {statValue(graph, kind)}
                </Badge>
              ))}
            </Group>

            <Text size="xs" c="dimmed">
              {t('map.dragHint')}
            </Text>
          </Group>

          {view === 'counters' && (graph.omittedUsers > 0 || graph.omittedGroups > 0) ? (
            <Alert color="gray" variant="light" icon={<IconAlertTriangle size={16} />}>
              <Text size="sm">
                {t('map.omitted', {
                  users: graph.omittedUsers,
                  groups: graph.omittedGroups,
                })}
              </Text>
            </Alert>
          ) : null}

          <MapLegend view={view} />

          <Paper withBorder radius="md" style={{ height: 'calc(100vh - 340px)', minHeight: 520 }}>
            <MapCanvas
              key={view}
              graph={graph}
              colorMode={colorScheme === 'dark' ? 'dark' : 'light'}
              onInit={setInstance}
            />
          </Paper>

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
