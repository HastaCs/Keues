import { Badge, Button, Drawer, Group, Stack, Text, ThemeIcon } from '@mantine/core';
import { IconAlertTriangle, IconCircleCheck } from '@tabler/icons-react';
import { useTranslation } from 'react-i18next';
import type { MapIssue } from './types';

interface IssuesDrawerProps {
  opened: boolean;
  issues: MapIssue[];
  onClose: () => void;
  onSelect: (nodeId: string) => void;
}

export function IssuesDrawer({ opened, issues, onClose, onSelect }: IssuesDrawerProps) {
  const { t } = useTranslation();

  return (
    <Drawer opened={opened} onClose={onClose} position="right" title={t('map.issuesTitle')}>
      <Stack gap="md">
        <Text size="sm" c="dimmed">
          {t('map.issuesDescription')}
        </Text>

        {issues.length === 0 ? (
          <Stack align="center" gap="xs" py="xl">
            <ThemeIcon variant="light" color="green" size={44} radius="xl">
              <IconCircleCheck size={22} />
            </ThemeIcon>
            <Text fw={600}>{t('map.noIssuesTitle')}</Text>
            <Text size="sm" c="dimmed" ta="center">
              {t('map.noIssuesDescription')}
            </Text>
          </Stack>
        ) : (
          <Stack gap="sm">
            {issues.map((issue) => (
              <Group key={issue.id} wrap="nowrap" align="flex-start" gap="sm">
                <ThemeIcon
                  variant="light"
                  color={issue.severity === 'error' ? 'red' : 'yellow'}
                  size={32}
                  radius="md"
                >
                  <IconAlertTriangle size={16} />
                </ThemeIcon>

                <Stack gap={4} style={{ minWidth: 0, flex: 1 }}>
                  <Text size="sm" fw={600}>
                    {t(`map.issues.${issue.code}`)}
                  </Text>

                  <Group gap={6}>
                    <Badge
                      size="xs"
                      variant="light"
                      color={issue.severity === 'error' ? 'red' : 'yellow'}
                    >
                      {t(`map.severity.${issue.severity}`)}
                    </Badge>

                    {issue.nodeId ? (
                      <Button
                        size="compact-xs"
                        variant="subtle"
                        onClick={() => {
                          onSelect(issue.nodeId as string);
                          onClose();
                        }}
                      >
                        {t('map.locate')}
                      </Button>
                    ) : null}
                  </Group>
                </Stack>
              </Group>
            ))}
          </Stack>
        )}
      </Stack>
    </Drawer>
  );
}
