import { Group, Paper, Text } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import type { MapEdgeCategory, MapNodeKind, MapView } from './types';

const VIEW_KINDS: Record<MapView, MapNodeKind[]> = {
  machines: ['flow', 'menu', 'ticket'],
  counters: ['counter', 'queue', 'group', 'user'],
};

const VIEW_EDGES: Record<MapView, MapEdgeCategory[]> = {
  machines: ['flow'],
  counters: ['link', 'person', 'membership'],
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

const EDGE_COLOR: Record<MapEdgeCategory, string> = {
  flow: 'var(--mantine-color-cyan-5)',
  link: 'var(--mantine-color-blue-5)',
  person: 'var(--mantine-color-grape-5)',
  membership: 'var(--mantine-color-grape-4)',
};

const DASHED_EDGES: MapEdgeCategory[] = ['person', 'membership'];

export function MapLegend({ view }: { view: MapView }) {
  const { t } = useTranslation();

  return (
    <Paper withBorder radius="md" p="sm">
      <Group gap="lg" wrap="wrap">
        {VIEW_KINDS[view].map((kind) => (
          <Group key={kind} gap={6} wrap="nowrap">
            <span
              style={{
                width: 10,
                height: 10,
                borderRadius: '50%',
                backgroundColor: `var(--mantine-color-${KIND_COLOR[kind]}-6)`,
              }}
            />
            <Text size="xs" c="dimmed">
              {t(`map.kinds.${kind}`)}
            </Text>
          </Group>
        ))}

        {VIEW_EDGES[view].map((category) => (
          <Group key={category} gap={6} wrap="nowrap">
            <span
              style={{
                width: 22,
                height: 0,
                borderTop: `2px ${DASHED_EDGES.includes(category) ? 'dashed' : 'solid'} ${EDGE_COLOR[category]}`,
              }}
            />
            <Text size="xs" c="dimmed">
              {t(`map.edges.${category}`)}
            </Text>
          </Group>
        ))}
      </Group>
    </Paper>
  );
}
