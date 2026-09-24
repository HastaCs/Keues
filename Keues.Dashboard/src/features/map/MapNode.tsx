import { Badge, Group, Paper, Stack, Text, ThemeIcon } from '@mantine/core';
import { Handle, Position, type NodeProps } from '@xyflow/react';
import {
  IconAlertTriangle,
  IconArmchair,
  IconCategory,
  IconFolder,
  IconGitBranch,
  IconTicket,
  IconUser,
  IconUsersGroup,
} from '@tabler/icons-react';
import {
  useRef,
  type ComponentType,
  type KeyboardEvent as ReactKeyboardEvent,
  type MouseEvent as ReactMouseEvent,
  type PointerEvent as ReactPointerEvent,
} from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useActiveLocation } from '@/features/locations/LocationContext';
import { NODE_SHAPES } from './nodeShapes';
import type { MapGraphNode, MapNodeKind } from './types';

const KIND_ICON: Record<MapNodeKind, ComponentType<{ size?: number }>> = {
  flow: IconGitBranch,
  menu: IconFolder,
  ticket: IconTicket,
  queue: IconCategory,
  counter: IconArmchair,
  group: IconUsersGroup,
  user: IconUser,
};

const KIND_COLOR: Record<MapNodeKind, string> = {
  flow: 'cyan',
  menu: 'orange',
  ticket: 'blue',
  queue: 'blue',
  counter: 'teal',
  group: 'grape',
  user: 'indigo',
};

interface HandleSpec {
  id: string;
  position: Position;
}

const KIND_HANDLES: Record<MapNodeKind, { target: HandleSpec[]; source: HandleSpec[] }> = {
  flow: { target: [], source: [{ id: 'down', position: Position.Bottom }] },
  menu: {
    target: [{ id: 'top', position: Position.Top }],
    source: [{ id: 'down', position: Position.Bottom }],
  },
  ticket: {
    target: [{ id: 'top', position: Position.Top }],
    source: [{ id: 'down', position: Position.Bottom }],
  },
  queue: { target: [{ id: 'top', position: Position.Top }], source: [] },
  counter: {
    target: [],
    source: [
      { id: 'up', position: Position.Top },
      { id: 'down', position: Position.Bottom },
    ],
  },
  group: {
    target: [{ id: 'bottom', position: Position.Bottom }],
    source: [{ id: 'top', position: Position.Top }],
  },
  user: { target: [{ id: 'bottom', position: Position.Bottom }], source: [] },
};

function getFlowTypeLabelKey(flowType: number): string {
  if (flowType === 1) {
    return 'flows.SetFree';
  }
  if (flowType === 2) {
    return 'flows.ManualCall';
  }
  return 'flows.TicketMachine';
}

function buildTarget(route: string, entityId?: string): string {
  if (!entityId) {
    return route;
  }

  switch (route) {
    case 'groups':
      return `groups/${entityId}`;
    case 'flows':
      return `flows?flow=${entityId}`;
    case 'counters':
      return `counters?open=${entityId}`;
    case 'ticketTypes':
      return `ticketTypes?open=${entityId}`;
    case 'users':
      return `users?open=${entityId}`;
    default:
      return route;
  }
}

export function MapNode({ data }: NodeProps<MapGraphNode>) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useActiveLocation();
  const pointerStart = useRef<{ x: number; y: number } | null>(null);
  const Icon = KIND_ICON[data.kind];
  const color = data.color ?? KIND_COLOR[data.kind];
  const handles = KIND_HANDLES[data.kind];

  const kindLabel =
    data.kind === 'flow' ? t(getFlowTypeLabelKey(data.flowType ?? 0)) : t(`map.kinds.${data.kind}`);

  function handleOpen() {
    if (data.route && location) {
      navigate(`/locations/${location.id}/${buildTarget(data.route, data.entityId)}`);
    }
  }

  function handlePointerDown(event: ReactPointerEvent) {
    pointerStart.current = { x: event.clientX, y: event.clientY };
  }

  function handleClick(event: ReactMouseEvent) {
    const start = pointerStart.current;
    pointerStart.current = null;

    if (start && Math.abs(event.clientX - start.x) + Math.abs(event.clientY - start.y) > 6) {
      return;
    }

    handleOpen();
  }

  function handleKeyDown(event: ReactKeyboardEvent) {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      handleOpen();
    }
  }

  const shape = NODE_SHAPES[data.kind];
  const colorVar = `var(--mantine-color-${color}-6)`;

  const content = (
    <Group wrap="nowrap" gap="sm" align="flex-start">
      <ThemeIcon
        variant="light"
        color={color}
        radius={data.kind === 'user' ? 'xl' : 'md'}
        size={36}
      >
        <Icon size={18} />
      </ThemeIcon>

      <Stack gap={2} style={{ minWidth: 0, flex: 1 }}>
        <Text size="sm" fw={700} truncate>
          {data.label}
        </Text>

        {data.subtitle ? (
          <Text size="xs" c="dimmed" truncate>
            {data.subtitle}
          </Text>
        ) : null}

        <Group gap={4} wrap="wrap">
          {data.kind !== 'user' ? (
            <Badge size="xs" variant="light" color={color}>
              {kindLabel}
            </Badge>
          ) : null}

          {data.queueLabel ? (
            <Badge size="xs" variant="light" color="blue" leftSection={<IconCategory size={10} />}>
              {data.queueLabel}
            </Badge>
          ) : null}

          {data.disabled ? (
            <Badge size="xs" variant="light" color="gray">
              {t('map.disabled')}
            </Badge>
          ) : null}

          {data.issueCount > 0 ? (
            <Badge
              size="xs"
              variant="light"
              color="red"
              leftSection={<IconAlertTriangle size={10} />}
            >
              {data.issueCount}
            </Badge>
          ) : null}
        </Group>
      </Stack>
    </Group>
  );

  return (
    <div style={{ width: '100%' }}>
      {handles.target.map((handle) => (
        <Handle key={handle.id} id={handle.id} type="target" position={handle.position} />
      ))}

      {shape.clipPath ? (
        <div
          role="button"
          tabIndex={0}
          onPointerDown={handlePointerDown}
          onClick={handleClick}
          onKeyDown={handleKeyDown}
          style={{
            clipPath: shape.clipPath,
            backgroundColor: colorVar,
            padding: shape.ring ?? 2,
            filter: shape.glow ? `drop-shadow(0 0 6px ${colorVar})` : undefined,
            cursor: data.route ? 'pointer' : 'default',
          }}
        >
          <div
            style={{
              clipPath: shape.clipPath,
              backgroundColor: 'var(--mantine-color-body)',
              padding: shape.padding,
            }}
          >
            {content}
          </div>
        </div>
      ) : (
        <Paper
          withBorder
          radius={shape.borderRadius}
          onPointerDown={handlePointerDown}
          onClick={handleClick}
          style={{
            width: '100%',
            padding: shape.padding,
            borderLeft: shape.accentLeft ? `6px solid ${colorVar}` : undefined,
            borderStyle: shape.dashed ? 'dashed' : undefined,
            cursor: data.route ? 'pointer' : 'default',
            backgroundColor: 'var(--mantine-color-body)',
          }}
        >
          {content}
        </Paper>
      )}

      {handles.source.map((handle) => (
        <Handle key={handle.id} id={handle.id} type="source" position={handle.position} />
      ))}
    </div>
  );
}
