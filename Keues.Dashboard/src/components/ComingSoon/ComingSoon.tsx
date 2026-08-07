import { Paper, Stack, Text, ThemeIcon, Title } from '@mantine/core';
import { IconTools } from '@tabler/icons-react';
import { useTranslation } from 'react-i18next';

interface ComingSoonProps {
  moduleKey: string;
}

export function ComingSoon({ moduleKey }: ComingSoonProps) {
  const { t } = useTranslation();

  return (
    <Paper withBorder radius="md" p="xl">
      <Stack align="center" gap="sm" py="xl">
        <ThemeIcon size={64} radius="xl" variant="light" color="gray">
          <IconTools size={32} />
        </ThemeIcon>

        <Title order={2}>{t(moduleKey)}</Title>

        <Text c="dimmed" ta="center">
          {t('comingSoon.description')}
        </Text>
      </Stack>
    </Paper>
  );
}
