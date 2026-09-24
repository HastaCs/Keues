import { describe, expect, it } from 'vitest';
import type { Counter } from '@/api/interfaces/Counter/Counters';
import type { Flow, FlowMenuItem } from '@/api/interfaces/Flow/Flows';
import type { Queue } from '@/api/interfaces/Queue/Queues';
import type { User } from '@/api/interfaces/User/Users';
import type { UserGroup } from '@/api/interfaces/UserGroup/UserGroups';
import { buildCounterGraph, buildFlowGraph, type MapSourceData } from './graphBuilder';

const LOCATION = 'loc-1';

function makeQueue(overrides: Partial<Queue> & { id: string }): Queue {
  return {
    name: `Queue ${overrides.id}`,
    description: '',
    maxValue: null,
    code: 'Q',
    priority: 1,
    weight: 1,
    agingIntervalMinutes: 0,
    maxAgingBonus: 0,
    color: 'blue',
    locationId: LOCATION,
    counters: [],
    createdAt: '2026-01-01T00:00:00Z',
    resetAt: null,
    ...overrides,
  };
}

function makeCounter(overrides: Partial<Counter> & { id: string }): Counter {
  return {
    code: 'C',
    name: `Counter ${overrides.id}`,
    color: 'teal',
    description: '',
    locationId: LOCATION,
    queues: [],
    createdAt: '2026-01-01T00:00:00Z',
    authorizedUsers: [],
    authorizedUserGroups: [],
    ...overrides,
  };
}

function makeUser(overrides: Partial<User> & { id: string }): User {
  return {
    name: `User ${overrides.id}`,
    email: `${overrides.id}@keues.dev`,
    locationId: LOCATION,
    createdAt: '2026-01-01T00:00:00Z',
    enabled: true,
    ...overrides,
  };
}

function makeGroup(overrides: Partial<UserGroup> & { id: string }): UserGroup {
  return {
    name: `Group ${overrides.id}`,
    color: 'grape',
    locationId: LOCATION,
    createdAt: '2026-01-01T00:00:00Z',
    userIds: [],
    ...overrides,
  };
}

function makeMenuItem(overrides: Partial<FlowMenuItem> & { id: string }): FlowMenuItem {
  return {
    name: `Item ${overrides.id}`,
    description: '',
    nodeType: 'ticket',
    parentId: null,
    queueSystemId: 'sys',
    queueId: null,
    icon: 'ticket',
    color: 'blue',
    removedAt: null,
    ...overrides,
  };
}

function makeFlow(overrides: Partial<Flow> & { id: string }): Flow {
  return {
    name: `Flow ${overrides.id}`,
    description: '',
    flowType: 0,
    locationId: LOCATION,
    menuItems: [],
    createdAt: '2026-01-01T00:00:00Z',
    flowJson: '[]',
    ...overrides,
  };
}

function baseSource(overrides: Partial<MapSourceData> = {}): MapSourceData {
  return {
    queues: [],
    counters: [],
    users: [],
    groups: [],
    flows: [],
    ...overrides,
  };
}

function edgeKeys(graph: ReturnType<typeof buildFlowGraph>): string[] {
  return graph.edges.map((edge) => `${edge.source}->${edge.target}`).sort();
}

function issueCodes(graph: ReturnType<typeof buildFlowGraph>): string[] {
  return graph.issues.map((issue) => issue.code).sort();
}

describe('buildFlowGraph', () => {
  it('builds a machine → menu → ticket tree without queue nodes', () => {
    const graph = buildFlowGraph(
      baseSource({
        queues: [makeQueue({ id: 'q1' })],
        flows: [
          makeFlow({
            id: 'f1',
            menuItems: [
              makeMenuItem({ id: 'm1', nodeType: 'menu', name: 'Bebidas' }),
              makeMenuItem({ id: 't1', parentId: 'm1', nodeType: 'ticket', queueId: 'q1' }),
            ],
          }),
        ],
      })
    );

    expect(edgeKeys(graph)).toEqual(['flow:f1->item:f1:m1', 'item:f1:m1->item:f1:t1']);
    expect(graph.nodes.some((node) => node.data.kind === 'queue')).toBe(false);
    expect(graph.stats.menus).toBe(1);
    expect(graph.stats.tickets).toBe(1);
    expect(graph.nodes.find((node) => node.id === 'item:f1:t1')?.data.queueLabel).toBe(
      'Q · Queue q1'
    );
  });

  it('attaches root items directly to the machine', () => {
    const graph = buildFlowGraph(
      baseSource({
        flows: [makeFlow({ id: 'f1', menuItems: [makeMenuItem({ id: 't1' })] })],
      })
    );

    expect(edgeKeys(graph)).toEqual(['flow:f1->item:f1:t1']);
  });

  it('flags a machine without menus or tickets', () => {
    const graph = buildFlowGraph(baseSource({ flows: [makeFlow({ id: 'f1', flowType: 1 })] }));

    expect(issueCodes(graph)).toContain('flowWithoutTickets');
  });

  it('flags a ticket pointing to a missing queue', () => {
    const graph = buildFlowGraph(
      baseSource({
        flows: [
          makeFlow({ id: 'f1', menuItems: [makeMenuItem({ id: 't1', queueId: 'missing' })] }),
        ],
      })
    );

    expect(issueCodes(graph)).toContain('missingQueueReference');
  });

  it('ignores removed menu items', () => {
    const graph = buildFlowGraph(
      baseSource({
        flows: [
          makeFlow({
            id: 'f1',
            menuItems: [makeMenuItem({ id: 't1', removedAt: '2026-01-02T00:00:00Z' })],
          }),
        ],
      })
    );

    expect(graph.nodes.some((node) => node.id === 'item:f1:t1')).toBe(false);
    expect(issueCodes(graph)).toContain('flowWithoutTickets');
  });
});

describe('buildCounterGraph', () => {
  it('unions queue.counters and counter.queues when building links', () => {
    const graph = buildCounterGraph(
      baseSource({
        queues: [makeQueue({ id: 'q1', counters: ['c1'] }), makeQueue({ id: 'q2' })],
        counters: [makeCounter({ id: 'c1' }), makeCounter({ id: 'c2', queues: ['q2'] })],
      })
    );

    expect(edgeKeys(graph)).toEqual(['counter:c1->queue:c1:q1', 'counter:c2->queue:c2:q2']);
  });

  it('flags counters without any queue', () => {
    const graph = buildCounterGraph(baseSource({ counters: [makeCounter({ id: 'c1' })] }));

    expect(issueCodes(graph)).toContain('counterWithoutQueue');
  });

  it('flags queues without any counter', () => {
    const graph = buildCounterGraph(baseSource({ queues: [makeQueue({ id: 'q1' })] }));

    expect(issueCodes(graph)).toContain('queueWithoutCounter');
  });

  it('flags mismatched bidirectional links', () => {
    const graph = buildCounterGraph(
      baseSource({
        queues: [makeQueue({ id: 'q1' })],
        counters: [makeCounter({ id: 'c1', queues: ['q1'] })],
      })
    );

    expect(issueCodes(graph)).toContain('mismatchedLinks');
  });

  it('shows related users and groups and counts the omitted ones', () => {
    const graph = buildCounterGraph(
      baseSource({
        queues: [makeQueue({ id: 'q1', counters: ['c1'] })],
        counters: [
          makeCounter({
            id: 'c1',
            queues: ['q1'],
            authorizedUsers: [{ id: 'u1' }],
            authorizedUserGroups: [{ id: 'g1' }],
          }),
        ],
        users: [makeUser({ id: 'u1' }), makeUser({ id: 'u2' })],
        groups: [makeGroup({ id: 'g1', userIds: [{ id: 'u3', name: 'User u3' }] })],
      })
    );

    expect(edgeKeys(graph)).toEqual([
      'counter:c1->group:c1:g1',
      'counter:c1->queue:c1:q1',
      'counter:c1->user:c1:u1',
      'group:c1:g1->user:c1:u3',
    ]);
    expect(graph.nodes.some((node) => node.id === 'user:c1:u2')).toBe(false);
    expect(graph.omittedUsers).toBe(1);
    expect(graph.omittedGroups).toBe(0);
  });

  it('duplicates shared queues per counter', () => {
    const graph = buildCounterGraph(
      baseSource({
        queues: [makeQueue({ id: 'q1', counters: ['c1', 'c2'] })],
        counters: [makeCounter({ id: 'c1' }), makeCounter({ id: 'c2' })],
      })
    );

    expect(edgeKeys(graph)).toEqual(['counter:c1->queue:c1:q1', 'counter:c2->queue:c2:q1']);
    expect(graph.nodes.filter((node) => node.data.kind === 'queue')).toHaveLength(2);
  });

  it('only includes the selected counters', () => {
    const graph = buildCounterGraph(
      baseSource({
        queues: [makeQueue({ id: 'q1', counters: ['c1', 'c2'] })],
        counters: [makeCounter({ id: 'c1' }), makeCounter({ id: 'c2' })],
      }),
      ['c2']
    );

    expect(edgeKeys(graph)).toEqual(['counter:c2->queue:c2:q1']);
    expect(graph.nodes.some((node) => node.id === 'counter:c1')).toBe(false);
    expect(graph.stats.counters).toBe(1);
  });

  it('creates a ghost user when an authorized user is missing', () => {
    const graph = buildCounterGraph(
      baseSource({
        counters: [makeCounter({ id: 'c1', authorizedUsers: [{ id: 'ghost' }] })],
      })
    );

    expect(graph.nodes.some((node) => node.id === 'user:c1:ghost')).toBe(true);
    expect(issueCodes(graph)).toContain('ghostUser');
  });
});
