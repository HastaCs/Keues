import { Paper, Stack, Text } from '@mantine/core';
import type { ReactNode } from 'react';

interface EmptyStateProps {
  title: string;
  description?: string;
  action?: ReactNode;
}

export function EmptyState({ title, description, action }: EmptyStateProps) {
  return (
    <Paper withBorder radius="md" p="xl">
      <Stack align="center" gap={6}>
        <Text fw={600}>{title}</Text>

        {description ? (
          <Text c="dimmed" ta="center">
            {description}
          </Text>
        ) : null}

        {action ? <div style={{ marginTop: 8 }}>{action}</div> : null}
      </Stack>
    </Paper>
  );
}
