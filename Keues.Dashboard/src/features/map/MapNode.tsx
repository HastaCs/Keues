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

const TARGET_KINDS: MapNodeKind[] = ['menu', 'ticket', 'queue', 'group', 'user'];
const SOURCE_KINDS: MapNodeKind[] = ['flow', 'menu', 'ticket', 'counter', 'group'];

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
  const showTarget = TARGET_KINDS.includes(data.kind);
  const showSource = SOURCE_KINDS.includes(data.kind);

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
      {showTarget ? <Handle type="target" position={Position.Top} /> : null}

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
            padding: 2,
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

      {showSource ? <Handle type="source" position={Position.Bottom} /> : null}
    </div>
  );
}
