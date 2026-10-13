import { Box, Button, Stack, Text, Tooltip } from '@mantine/core';
import { useFullscreenElement } from '@mantine/hooks';
import { IconMaximize } from '@tabler/icons-react';
import { useTranslation } from 'react-i18next';
import { primeAudio } from './sound';
import type { CalledTurn, MonitorStatus } from './useMonitorConnection';

interface MonitorScreenProps {
  status: MonitorStatus;
  turns: CalledTurn[];
}

const STATUS_COLOR: Record<MonitorStatus, string> = {
  connecting: 'yellow',
  connected: 'green',
  reconnecting: 'orange',
  disconnected: 'red',
};

export function MonitorScreen({ status, turns }: MonitorScreenProps) {
  const { t } = useTranslation();
  const { ref, toggle, fullscreen } = useFullscreenElement<HTMLDivElement>();

  const latest = turns[0];
  const statusLabel = t(`flows.manualMonitorStatus.${status}`);

  return (
    <div
      ref={ref}
      style={{
        display: 'flex',
        flexDirection: 'column',
        minHeight: fullscreen ? '100vh' : '65vh',
        padding: '1.5rem',
        borderRadius: fullscreen ? 0 : 'var(--mantine-radius-md)',
        backgroundColor: 'var(--mantine-color-body)',
      }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Tooltip label={statusLabel} withArrow>
          <Box
            w={14}
            h={14}
            style={{
              borderRadius: '50%',
              backgroundColor: `var(--mantine-color-${STATUS_COLOR[status]}-6)`,
            }}
          />
        </Tooltip>

        {fullscreen ? null : (
          <Button
            variant="default"
            leftSection={<IconMaximize size={16} />}
            onClick={() => {
              primeAudio();
              void toggle();
            }}
          >
            {t('flows.monitorFullscreen')}
          </Button>
        )}
      </div>

      <Stack align="center" justify="center" gap="sm" style={{ flex: 1 }}>
        {latest ? (
          <>
            <Text
              fw={800}
              style={{ fontSize: 'clamp(5rem, 26vw, 30rem)', lineHeight: 1, textAlign: 'center' }}
            >
              {latest.code}
            </Text>

            {latest.counterCode ? (
              <Text
                c="dimmed"
                style={{ fontSize: 'clamp(1.5rem, 4vw, 3.5rem)', textAlign: 'center' }}
              >
                {t('flows.manualMonitorCounter', { counter: latest.counterCode })}
              </Text>
            ) : null}
          </>
        ) : (
          <Text c="dimmed" style={{ fontSize: 'clamp(2rem, 9vw, 7rem)', textAlign: 'center' }}>
            {t('flows.manualMonitorWaiting')}
          </Text>
        )}
      </Stack>
    </div>
  );
}
