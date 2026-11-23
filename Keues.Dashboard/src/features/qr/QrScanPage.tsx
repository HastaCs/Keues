import {
  Alert,
  Badge,
  Button,
  Container,
  Group,
  Loader,
  Paper,
  SegmentedControl,
  Stack,
  Text,
  ThemeIcon,
  Title,
} from '@mantine/core';
import { IconAlertTriangle, IconBellRinging, IconTicket } from '@tabler/icons-react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import { getErrorMessage } from '@/api/getErrorMessage';
import { ApiError } from '@/api/httpClient';
import {
  getQrTicketStatus,
  isFinishedTicketStatus,
  issueQrTicket,
  QR_TICKET_STATUS,
  QrFlowError,
  type QrIssueResult,
} from '@/api/qrApi';
import {
  clearFinishedQrTicket,
  clearQrTicket,
  getFinishedQrTicket,
  getStoredQrTicket,
  saveFinishedQrTicket,
  saveQrTicket,
  type StoredQrTicket,
} from './qrTicketStorage';
import styles from './QrScanPage.module.css';

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function isReloadNavigation() {
  const [entry] = performance.getEntriesByType('navigation') as PerformanceNavigationTiming[];
  return entry?.type === 'reload' || entry?.type === 'back_forward';
}

export function QrScanPage() {
  const { t, i18n } = useTranslation();
  const language = i18n.resolvedLanguage === 'en' ? 'en' : 'es';
  const { flowId = '', token: tokenParam } = useParams();
  const [searchParams] = useSearchParams();
  const token = tokenParam ?? searchParams.get('token') ?? '';
  const hasValidFlowId = UUID_PATTERN.test(flowId);
  const [isReload] = useState(isReloadNavigation);
  const navigate = useNavigate();

  const [storedTicket, setStoredTicket] = useState<StoredQrTicket | null>(() =>
    hasValidFlowId ? getStoredQrTicket(flowId) : null
  );
  const [finishedTicket, setFinishedTicket] = useState<StoredQrTicket | null>(() =>
    hasValidFlowId && (!token || isReload) ? getFinishedQrTicket(flowId) : null
  );
  const [checking, setChecking] = useState(false);
  const firstRender = useRef(true);

  useEffect(() => {
    if (firstRender.current) {
      firstRender.current = false;
      return;
    }

    setStoredTicket(hasValidFlowId ? getStoredQrTicket(flowId) : null);
    setFinishedTicket(null);
    autoIssueStarted.current = false;
  }, [flowId, hasValidFlowId]);

  useEffect(() => {
    const storedLanguage = window.localStorage.getItem('keues-qr-language');
    if ((storedLanguage === 'es' || storedLanguage === 'en') && storedLanguage !== language) {
      void i18n.changeLanguage(storedLanguage);
    }
  }, [i18n, language]);

  function handleLanguageChange(value: string) {
    if (value !== 'es' && value !== 'en') {
      return;
    }

    window.localStorage.setItem('keues-qr-language', value);
    void i18n.changeLanguage(value);
  }

  const issue = useMutation<QrIssueResult, unknown, void>({
    mutationFn: () => issueQrTicket(flowId, token),
    onSuccess: (result) => {
      const stored: StoredQrTicket = {
        id: result.ticket.id,
        code: result.ticket.code,
        flowId,
        flowName: result.flowName,
        createdAt: new Date().toISOString(),
      };

      saveQrTicket(stored);
      clearFinishedQrTicket(flowId);
      setFinishedTicket(null);
      setStoredTicket(stored);
    },
  });

  const issueRef = useRef(issue.mutate);
  issueRef.current = issue.mutate;

  const autoIssueStarted = useRef(false);

  useEffect(() => {
    if (!hasValidFlowId || !token || storedTicket || isReload || autoIssueStarted.current) {
      return;
    }

    autoIssueStarted.current = true;
    issueRef.current();
  }, [hasValidFlowId, isReload, token, storedTicket]);

  const statusQuery = useQuery({
    queryKey: ['qr-ticket-status', storedTicket?.id],
    queryFn: () => getQrTicketStatus(storedTicket!.id),
    enabled: Boolean(storedTicket?.id),
    retry: 1,
    staleTime: 0,
    refetchOnMount: 'always',
    refetchOnWindowFocus: true,
    refetchInterval: (query) => {
      const data = query.state.data;
      return data && isFinishedTicketStatus(data.status) ? false : 5000;
    },
  });

  useEffect(() => {
    function handlePageShow(event: PageTransitionEvent) {
      if (!event.persisted) {
        return;
      }

      setChecking(true);
      void statusQuery.refetch().finally(() => setChecking(false));
    }

    window.addEventListener('pageshow', handlePageShow);
    return () => window.removeEventListener('pageshow', handlePageShow);
  }, [statusQuery.refetch]);

  useEffect(() => {
    if (
      !storedTicket ||
      !(statusQuery.error instanceof ApiError) ||
      statusQuery.error.status !== 400
    ) {
      return;
    }

    clearQrTicket(flowId);
    setStoredTicket(null);
    autoIssueStarted.current = true;

    if (token && !isReload) {
      issueRef.current();
    }
  }, [flowId, isReload, statusQuery.error, storedTicket, token]);

  useEffect(() => {
    const status = statusQuery.data?.status;

    if (!storedTicket || status === undefined || !isFinishedTicketStatus(status)) {
      return;
    }

    clearQrTicket(flowId);
    saveFinishedQrTicket(storedTicket);
    setFinishedTicket(storedTicket);
    setStoredTicket(null);
    navigate(`/turno/${flowId}`, { replace: true });
  }, [flowId, navigate, statusQuery.data, storedTicket]);

  function resolveIssueError(error: unknown) {
    if (error instanceof QrFlowError) {
      return error.code === 'invalid-flow' ? t('qr.invalidFlow') : t('qr.missingQueue');
    }

    if (error instanceof ApiError && error.status === 400) {
      return t('qr.expiredToken');
    }

    return getErrorMessage(error, t('errors.unexpected'));
  }

  const ticketStatus = statusQuery.data?.status;
  const isInProgress = ticketStatus === QR_TICKET_STATUS.inProgress;
  const counterCode = statusQuery.data?.counterCode;
  const statusPending = Boolean(
    storedTicket && (checking || (!statusQuery.isFetchedAfterMount && !statusQuery.isError))
  );

  const loadingView = (
    <>
      <Loader size="lg" />
      <Text fw={500}>{t('qr.issuingTitle')}</Text>
      <Text ta="center" c="dimmed">
        {t('qr.issuingMessage')}
      </Text>
    </>
  );

  return (
    <Container size="xs" py="xl">
      <Group justify="flex-end" mb="sm">
        <SegmentedControl
          size="xs"
          radius="xl"
          value={language}
          onChange={handleLanguageChange}
          data={[
            { value: 'es', label: t('qr.languageEs') },
            { value: 'en', label: t('qr.languageEn') },
          ]}
        />
      </Group>

      <Paper
        withBorder
        radius="lg"
        p="xl"
        mt="xl"
        style={
          isInProgress
            ? {
                borderColor: 'var(--mantine-color-teal-5)',
                borderWidth: 2,
                boxShadow: 'var(--mantine-shadow-md)',
              }
            : undefined
        }
      >
        <Stack align="center" gap="md">
          {!hasValidFlowId ? (
            <Alert color="red" icon={<IconAlertTriangle size={18} />}>
              {t('qr.invalidLink')}
            </Alert>
          ) : storedTicket ? (
            statusPending ? (
              loadingView
            ) : (
              <>
                <ThemeIcon
                  size={isInProgress ? 80 : 64}
                  radius="xl"
                  variant={isInProgress ? 'filled' : 'light'}
                  color="teal"
                  className={isInProgress ? styles.pulse : undefined}
                >
                  {isInProgress ? <IconBellRinging size={44} /> : <IconTicket size={36} />}
                </ThemeIcon>

                <Text c="dimmed">
                  {storedTicket.flowName
                    ? t('qr.ticketFor', { name: storedTicket.flowName })
                    : t('qr.yourTurn')}
                </Text>

                <Title
                  order={1}
                  c={isInProgress ? 'teal.7' : undefined}
                  style={{
                    fontSize: isInProgress
                      ? 'clamp(2.5rem, 12vw, 4.5rem)'
                      : 'clamp(4rem, 22vw, 9rem)',
                    lineHeight: 1,
                  }}
                >
                  {storedTicket.code}
                </Title>

                {isInProgress ? (
                  <Alert
                    color="teal"
                    variant="light"
                    radius="lg"
                    w="100%"
                    icon={<IconBellRinging size={28} />}
                    title={
                      <Badge color="teal" variant="filled" size="lg" radius="xl" tt="uppercase">
                        {t('qr.inProgressTitle')}
                      </Badge>
                    }
                  >
                    <Stack align="center" gap={2} mt="xs">
                      {counterCode ? (
                        <>
                          <Text size="sm" tt="uppercase" fw={800} c="teal.7">
                            {t('qr.counterLabel')}
                          </Text>
                          <Title order={1} className={styles.counterName} c="teal.9">
                            {counterCode}
                          </Title>
                        </>
                      ) : (
                        <Text ta="center" fw={600}>
                          {t('qr.inProgressNoCounter')}
                        </Text>
                      )}
                    </Stack>
                  </Alert>
                ) : (
                  <Text ta="center" c="dimmed">
                    {t('qr.ticketMessage')}
                  </Text>
                )}

                {statusQuery.isLoading ? (
                  <Text size="xs" c="dimmed">
                    {t('qr.checkingStatus')}
                  </Text>
                ) : null}

                {statusQuery.isError ? (
                  <Alert color="orange" icon={<IconAlertTriangle size={18} />}>
                    {t('qr.statusError')}
                  </Alert>
                ) : null}
              </>
            )
          ) : finishedTicket ? (
            <>
              <ThemeIcon size={64} radius="xl" variant="light" color="gray">
                <IconTicket size={36} />
              </ThemeIcon>

              <Text c="dimmed">
                {finishedTicket.flowName
                  ? t('qr.ticketFor', { name: finishedTicket.flowName })
                  : t('qr.yourTurn')}
              </Text>

              <Title
                order={1}
                c="dimmed"
                style={{ fontSize: 'clamp(3rem, 16vw, 6rem)', lineHeight: 1 }}
              >
                {finishedTicket.code}
              </Title>

              <Title order={3} ta="center">
                {t('qr.finishedTitle')}
              </Title>
              <Text ta="center" c="dimmed">
                {t('qr.finishedMessage')}
              </Text>
            </>
          ) : issue.isError ? (
            <>
              <Alert color="red" title={t('qr.errorTitle')} icon={<IconAlertTriangle size={18} />}>
                {resolveIssueError(issue.error)}
              </Alert>
              <Button onClick={() => issue.mutate()}>{t('qr.retry')}</Button>
            </>
          ) : !token ? (
            <Alert color="red" icon={<IconAlertTriangle size={18} />}>
              {t('qr.missingToken')}
            </Alert>
          ) : isReload ? (
            <Alert color="blue" icon={<IconAlertTriangle size={18} />}>
              {t('qr.rescanRequired')}
            </Alert>
          ) : (
            loadingView
          )}
        </Stack>
      </Paper>
    </Container>
  );
}
