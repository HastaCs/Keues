import type { Edge } from '@xyflow/react';
import type { Counter } from '@/api/interfaces/Counter/Counters';
import type { Flow } from '@/api/interfaces/Flow/Flows';
import type { Queue } from '@/api/interfaces/Queue/Queues';
import type { User } from '@/api/interfaces/User/Users';
import type { UserGroup } from '@/api/interfaces/UserGroup/UserGroups';
import type {
  MapEdgeCategory,
  MapGraph,
  MapGraphNode,
  MapIssue,
  MapIssueCode,
  MapNodeInit,
  MapNodeKind,
} from './types';

export interface MapSourceData {
  queues: Queue[];
  counters: Counter[];
  users: User[];
  groups: UserGroup[];
  flows: Flow[];
}

const NODE_WIDTH = 240;

const NODE_HEIGHT: Record<MapNodeKind, number> = {
  flow: 92,
  menu: 84,
  ticket: 76,
  queue: 104,
  counter: 104,
  group: 88,
  user: 80,
};

const EDGE_STYLE: Record<MapEdgeCategory, { stroke: string; dashed: boolean }> = {
  flow: { stroke: 'var(--mantine-color-cyan-5)', dashed: false },
  link: { stroke: 'var(--mantine-color-blue-5)', dashed: false },
  person: { stroke: 'var(--mantine-color-grape-5)', dashed: true },
  membership: { stroke: 'var(--mantine-color-grape-4)', dashed: true },
};

const UP_KINDS: MapNodeKind[] = ['user', 'group'];

const EDGE_HANDLES: Record<MapEdgeCategory, { source: string; target: string }> = {
  flow: { source: 'down', target: 'top' },
  link: { source: 'down', target: 'top' },
  person: { source: 'up', target: 'bottom' },
  membership: { source: 'top', target: 'bottom' },
};

interface RawEdge {
  source: string;
  target: string;
  category: MapEdgeCategory;
}

interface BuilderState {
  source: MapSourceData;
  nodes: Map<string, MapGraphNode>;
  issues: MapIssue[];
  rawEdges: RawEdge[];
  edgeKeys: Set<string>;
  problemEdges: Set<string>;
  usersById: Map<string, User>;
  groupsById: Map<string, UserGroup>;
  queuesById: Map<string, Queue>;
  countersById: Map<string, Counter>;
  relatedUserIds: Set<string>;
  relatedGroupIds: Set<string>;
}

function symmetricDifference(left: Set<string>, right: Set<string>): string[] {
  const result: string[] = [];
  left.forEach((value) => {
    if (!right.has(value)) {
      result.push(value);
    }
  });
  right.forEach((value) => {
    if (!left.has(value)) {
      result.push(value);
    }
  });
  return result;
}

function createState(source: MapSourceData): BuilderState {
  return {
    source,
    nodes: new Map(),
    issues: [],
    rawEdges: [],
    edgeKeys: new Set(),
    problemEdges: new Set(),
    usersById: new Map(source.users.map((user) => [user.id, user])),
    groupsById: new Map(source.groups.map((group) => [group.id, group])),
    queuesById: new Map(source.queues.map((queue) => [queue.id, queue])),
    countersById: new Map(source.counters.map((counter) => [counter.id, counter])),
    relatedUserIds: new Set(),
    relatedGroupIds: new Set(),
  };
}

function addNode(state: BuilderState, id: string, data: MapNodeInit) {
  if (state.nodes.has(id)) {
    return;
  }

  state.nodes.set(id, {
    id,
    type: 'mapNode',
    position: { x: 0, y: 0 },
    data: { ...data, issueCount: 0 },
    style: { width: NODE_WIDTH },
  });
}

function addIssue(
  state: BuilderState,
  code: MapIssueCode,
  severity: MapIssue['severity'],
  nodeId?: string
) {
  const id = `${code}:${nodeId ?? 'global'}`;
  if (state.issues.some((issue) => issue.id === id)) {
    return;
  }

  state.issues.push({ id, code, severity, nodeId });
}

function addEdge(state: BuilderState, source: string, target: string, category: MapEdgeCategory) {
  const key = `${source}->${target}`;
  if (state.edgeKeys.has(key)) {
    return;
  }

  state.edgeKeys.add(key);
  state.rawEdges.push({ source, target, category });
}

function markProblem(state: BuilderState, source: string, target: string) {
  state.problemEdges.add(`${source}->${target}`);
}

function ensureCounterUserNode(
  state: BuilderState,
  counterId: string,
  userId: string,
  fallbackName?: string
): string {
  const nodeId = `user:${counterId}:${userId}`;
  state.relatedUserIds.add(userId);

  if (state.nodes.has(nodeId)) {
    return nodeId;
  }

  const user = state.usersById.get(userId);
  if (!user) {
    addIssue(state, 'ghostUser', 'error', nodeId);
  }

  addNode(state, nodeId, {
    kind: 'user',
    label: user?.name ?? fallbackName ?? userId,
    subtitle: user?.email,
    disabled: user ? !user.enabled : false,
    route: 'users',
    entityId: userId,
  });

  return nodeId;
}

function ensureCounterGroupNode(
  state: BuilderState,
  counterId: string,
  groupId: string,
  fallbackName?: string
): string {
  const nodeId = `group:${counterId}:${groupId}`;
  state.relatedGroupIds.add(groupId);

  if (state.nodes.has(nodeId)) {
    return nodeId;
  }

  const group = state.groupsById.get(groupId);
  if (!group) {
    addIssue(state, 'ghostGroup', 'error', nodeId);
  }

  addNode(state, nodeId, {
    kind: 'group',
    label: group?.name ?? fallbackName ?? groupId,
    color: group?.color,
    route: 'groups',
    entityId: groupId,
  });

  return nodeId;
}

function layoutGraph(state: BuilderState): MapGraph {
  const issueCountByNode = new Map<string, number>();
  state.issues.forEach((issue) => {
    if (issue.nodeId) {
      issueCountByNode.set(issue.nodeId, (issueCountByNode.get(issue.nodeId) ?? 0) + 1);
    }
  });

  const mainNodes = [...state.nodes.values()];

  mainNodes.forEach((node) => {
    node.data.issueCount = issueCountByNode.get(node.id) ?? 0;
  });

  const nodeById = new Map(mainNodes.map((node) => [node.id, node]));

  const parentOf = new Map<string, string>();
  const treeChildren = new Map<string, string[]>();

  state.rawEdges.forEach((edge) => {
    if (!nodeById.has(edge.source) || !nodeById.has(edge.target) || parentOf.has(edge.target)) {
      return;
    }

    parentOf.set(edge.target, edge.source);
    const children = treeChildren.get(edge.source) ?? [];
    children.push(edge.target);
    treeChildren.set(edge.source, children);
  });

  const H_GAP = 36;
  const V_GAP = 80;
  const MAX_ROW_WIDTH = 1500;

  interface Block {
    width: number;
    height: number;
    positions: Map<string, { x: number; y: number }>;
  }

  interface Arrangement {
    width: number;
    height: number;
    items: { block: Block; x: number; y: number }[];
  }

  const arrangeRows = (blocks: Block[], align: 'top' | 'bottom'): Arrangement => {
    const rows: Block[][] = [];
    let current: Block[] = [];
    let currentWidth = 0;

    blocks.forEach((block) => {
      const addition = (current.length > 0 ? H_GAP : 0) + block.width;

      if (current.length > 0 && currentWidth + addition > MAX_ROW_WIDTH) {
        rows.push(current);
        current = [block];
        currentWidth = block.width;
      } else {
        current.push(block);
        currentWidth += addition;
      }
    });

    if (current.length > 0) {
      rows.push(current);
    }

    const items: Arrangement['items'] = [];
    let y = 0;
    let width = 0;

    rows.forEach((row) => {
      const rowWidth = row.reduce(
        (acc, block, index) => acc + block.width + (index > 0 ? H_GAP : 0),
        0
      );
      const rowHeight = row.reduce((acc, block) => Math.max(acc, block.height), 0);
      let x = 0;

      row.forEach((block) => {
        items.push({ block, x, y: align === 'bottom' ? y + rowHeight - block.height : y });
        x += block.width + H_GAP;
      });

      width = Math.max(width, rowWidth);
      y += rowHeight + V_GAP;
    });

    return { width, height: rows.length > 0 ? y - V_GAP : 0, items };
  };

  const isUpKind = (id: string) => {
    const kind = nodeById.get(id)?.data.kind;
    return kind !== undefined && UP_KINDS.includes(kind);
  };

  const layoutNode = (nodeId: string): Block => {
    const node = nodeById.get(nodeId);
    const ownHeight = node ? NODE_HEIGHT[node.data.kind] : NODE_HEIGHT.user;
    const children = treeChildren.get(nodeId) ?? [];
    const upChildren = children.filter(isUpKind);
    const downChildren = children.filter((id) => !isUpKind(id));

    const upArrangement = arrangeRows(upChildren.map(layoutNode), 'bottom');
    const downArrangement = arrangeRows(downChildren.map(layoutNode), 'top');

    const width = Math.max(NODE_WIDTH, upArrangement.width, downArrangement.width);
    const positions = new Map<string, { x: number; y: number }>();

    const upOffsetX = (width - upArrangement.width) / 2;
    upArrangement.items.forEach((item) => {
      item.block.positions.forEach((position, id) => {
        positions.set(id, { x: upOffsetX + item.x + position.x, y: item.y + position.y });
      });
    });

    const nodeY = upArrangement.height > 0 ? upArrangement.height + V_GAP : 0;
    positions.set(nodeId, { x: (width - NODE_WIDTH) / 2, y: nodeY });

    const downY = nodeY + ownHeight + V_GAP;
    const downOffsetX = (width - downArrangement.width) / 2;
    downArrangement.items.forEach((item) => {
      item.block.positions.forEach((position, id) => {
        positions.set(id, {
          x: downOffsetX + item.x + position.x,
          y: downY + item.y + position.y,
        });
      });
    });

    const height =
      nodeY + ownHeight + (downArrangement.height > 0 ? V_GAP + downArrangement.height : 0);
    return { width, height, positions };
  };

  const roots = mainNodes.filter((node) => !parentOf.has(node.id));
  const BLOCK_GAP_X = 90;
  const BLOCK_GAP_Y = 130;
  const MAX_BLOCK_ROW_WIDTH = 3200;
  let cursorX = 0;
  let cursorY = 0;
  let rowHeight = 0;

  roots.forEach((root) => {
    const block = layoutNode(root.id);

    if (cursorX > 0 && cursorX + block.width > MAX_BLOCK_ROW_WIDTH) {
      cursorX = 0;
      cursorY += rowHeight + BLOCK_GAP_Y;
      rowHeight = 0;
    }

    block.positions.forEach((position, nodeId) => {
      const node = nodeById.get(nodeId);
      if (node) {
        node.position = { x: cursorX + position.x, y: cursorY + position.y };
      }
    });

    cursorX += block.width + BLOCK_GAP_X;
    rowHeight = Math.max(rowHeight, block.height);
  });

  const edges: Edge[] = state.rawEdges.map((edge) => {
    const key = `${edge.source}->${edge.target}`;
    const style = EDGE_STYLE[edge.category];
    const isProblem = state.problemEdges.has(key);

    return {
      id: key,
      source: edge.source,
      target: edge.target,
      sourceHandle: EDGE_HANDLES[edge.category].source,
      targetHandle: EDGE_HANDLES[edge.category].target,
      type: 'smoothstep',
      style: {
        stroke: isProblem ? 'var(--mantine-color-red-6)' : style.stroke,
        strokeWidth: isProblem ? 2.5 : 1.5,
        strokeDasharray: style.dashed ? '6 4' : undefined,
      },
    };
  });

  return {
    nodes: mainNodes,
    edges,
    issues: state.issues,
    stats: {
      flows: state.source.flows.length,
      menus: mainNodes.filter((node) => node.data.kind === 'menu').length,
      tickets: mainNodes.filter((node) => node.data.kind === 'ticket').length,
      queues: state.source.queues.length,
      counters: state.source.counters.length,
      groups: state.source.groups.length,
      users: state.source.users.length,
      issues: state.issues.length,
    },
    omittedUsers: state.source.users.filter((user) => !state.relatedUserIds.has(user.id)).length,
    omittedGroups: state.source.groups.filter((group) => !state.relatedGroupIds.has(group.id))
      .length,
  };
}

export function buildFlowGraph(source: MapSourceData, selectedFlowIds?: string[]): MapGraph {
  const flows = selectedFlowIds
    ? source.flows.filter((flow) => selectedFlowIds.includes(flow.id))
    : source.flows;
  const state = createState({ ...source, flows });

  flows.forEach((flow) => {
    const flowId = `flow:${flow.id}`;
    const items = (flow.menuItems ?? []).filter((item) => !item.removedAt);

    addNode(state, flowId, {
      kind: 'flow',
      label: flow.name,
      subtitle: flow.description || undefined,
      flowType: flow.flowType,
      route: 'flows',
      entityId: flow.id,
    });

    if (items.length === 0) {
      addIssue(state, 'flowWithoutTickets', 'warning', flowId);
    }

    const itemIds = new Set(items.map((item) => item.id));

    items.forEach((item) => {
      const itemId = `item:${flow.id}:${item.id}`;
      const kind: MapNodeKind = item.nodeType === 'menu' ? 'menu' : 'ticket';
      const queue =
        kind === 'ticket' && item.queueId ? state.queuesById.get(item.queueId) : undefined;

      addNode(state, itemId, {
        kind,
        label: item.name,
        color: item.color,
        route: 'flows',
        entityId: flow.id,
        queueLabel: queue ? `${queue.code} · ${queue.name}` : undefined,
      });

      if (item.parentId && itemIds.has(item.parentId)) {
        addEdge(state, `item:${flow.id}:${item.parentId}`, itemId, 'flow');
      } else {
        addEdge(state, flowId, itemId, 'flow');
      }

      if (kind === 'ticket' && !queue) {
        addIssue(state, 'missingQueueReference', 'error', itemId);
      }
    });
  });

  return layoutGraph(state);
}

export function buildCounterGraph(source: MapSourceData, selectedCounterIds?: string[]): MapGraph {
  const counters = selectedCounterIds
    ? source.counters.filter((counter) => selectedCounterIds.includes(counter.id))
    : source.counters;
  const state = createState({ ...source, counters });

  const hasOrphanQueue = source.queues.some((queue) => {
    const linked = new Set([
      ...(queue.counters ?? []),
      ...counters
        .filter((counter) => (counter.queues ?? []).includes(queue.id))
        .map((counter) => counter.id),
    ]);
    return linked.size === 0;
  });

  if (hasOrphanQueue) {
    addIssue(state, 'queueWithoutCounter', 'warning');
  }

  counters.forEach((counter) => {
    const counterId = `counter:${counter.id}`;
    const forward = new Set(counter.queues ?? []);
    const reverse = new Set(
      source.queues
        .filter((queue) => (queue.counters ?? []).includes(counter.id))
        .map((queue) => queue.id)
    );
    const effective = new Set([...forward, ...reverse]);
    const mismatched = symmetricDifference(forward, reverse);

    addNode(state, counterId, {
      kind: 'counter',
      label: `${counter.code} · ${counter.name}`,
      subtitle: counter.description || undefined,
      color: counter.color,
      route: 'counters',
      entityId: counter.id,
    });

    if (effective.size === 0) {
      addIssue(state, 'counterWithoutQueue', 'error', counterId);
    }

    if (mismatched.length > 0) {
      addIssue(state, 'mismatchedLinks', 'warning', counterId);
    }

    (counter.queues ?? []).forEach((queueId) => {
      if (!state.queuesById.has(queueId)) {
        addIssue(state, 'mismatchedLinks', 'warning', counterId);
      }
    });

    effective.forEach((queueId) => {
      const queue = state.queuesById.get(queueId);
      if (!queue) {
        addIssue(state, 'mismatchedLinks', 'warning', counterId);
        return;
      }

      const queueNodeId = `queue:${counter.id}:${queueId}`;

      addNode(state, queueNodeId, {
        kind: 'queue',
        label: queue.name,
        subtitle: queue.code,
        color: queue.color,
        route: 'ticketTypes',
        entityId: queueId,
      });

      addEdge(state, counterId, queueNodeId, 'link');

      if (mismatched.includes(queueId)) {
        markProblem(state, counterId, queueNodeId);
      }
    });

    (counter.authorizedUsers ?? []).forEach((authorized) => {
      const userNodeId = ensureCounterUserNode(state, counter.id, authorized.id);
      addEdge(state, counterId, userNodeId, 'person');
    });

    (counter.authorizedUserGroups ?? []).forEach((authorized) => {
      const groupNodeId = ensureCounterGroupNode(state, counter.id, authorized.id);
      addEdge(state, counterId, groupNodeId, 'person');

      const group = state.groupsById.get(authorized.id);
      (group?.userIds ?? []).forEach((member) => {
        const memberNodeId = ensureCounterUserNode(state, counter.id, member.id, member.name);
        addEdge(state, groupNodeId, memberNodeId, 'membership');
      });
    });
  });

  return layoutGraph(state);
}
