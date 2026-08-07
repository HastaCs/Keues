import { Group, Stack, Text } from '@mantine/core';
import type { ReactNode } from 'react';

interface PageHeaderProps {
  label?: string;
  title?: string;
  description?: string;
  actions?: ReactNode;
}

export function PageHeader({ label, title, description, actions }: PageHeaderProps) {
  return (
    <Group justify="space-between" align="flex-start">
      <Stack gap={4}>
        {label ? (
          <Text size="sm" c="dimmed" fw={600}>
            {label}
          </Text>
        ) : null}

        {title ? (
          <Text fw={700} size="xl">
            {title}
          </Text>
        ) : null}

        {description ? <Text c="dimmed">{description}</Text> : null}
      </Stack>

      {actions ? <Group>{actions}</Group> : null}
    </Group>
  );
}
