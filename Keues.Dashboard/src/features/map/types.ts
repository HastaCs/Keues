import type { Edge, Node } from '@xyflow/react';

export type MapNodeKind = 'flow' | 'menu' | 'ticket' | 'queue' | 'counter' | 'group' | 'user';

export type MapView = 'machines' | 'counters';

export type MapEdgeCategory = 'flow' | 'link' | 'person' | 'membership';

export type MapIssueCode =
  | 'queueWithoutCounter'
  | 'counterWithoutQueue'
  | 'flowWithoutTickets'
  | 'missingQueueReference'
  | 'mismatchedLinks'
  | 'ghostUser'
  | 'ghostGroup';

export interface MapIssue {
  id: string;
  code: MapIssueCode;
  severity: 'error' | 'warning';
  nodeId?: string;
}

export interface MapNodeData extends Record<string, unknown> {
  kind: MapNodeKind;
  label: string;
  subtitle?: string;
  color?: string;
  flowType?: number;
  disabled?: boolean;
  route?: string;
  entityId?: string;
  queueLabel?: string;
  issueCount: number;
}

export interface MapNodeInit {
  kind: MapNodeKind;
  label: string;
  subtitle?: string;
  color?: string;
  flowType?: number;
  disabled?: boolean;
  route?: string;
  entityId?: string;
  queueLabel?: string;
}

export type MapGraphNode = Node<MapNodeData, 'mapNode'>;

export interface MapStats {
  flows: number;
  menus: number;
  tickets: number;
  queues: number;
  counters: number;
  groups: number;
  users: number;
  issues: number;
}

export interface MapGraph {
  nodes: MapGraphNode[];
  edges: Edge[];
  issues: MapIssue[];
  stats: MapStats;
  omittedUsers: number;
  omittedGroups: number;
}
