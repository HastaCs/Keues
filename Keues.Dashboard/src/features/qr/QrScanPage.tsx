import {
  Alert,
  Button,
  Container,
  Loader,
  Paper,
  Stack,
  Text,
  ThemeIcon,
  Title,
} from '@mantine/core';
import { IconAlertTriangle, IconTicket } from '@tabler/icons-react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, useSearchParams } from 'react-router-dom';
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
  clearQrTicket,
  getStoredQrTicket,
  saveQrTicket,
  type StoredQrTicket,
} from './qrTicketStorage';

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function QrScanPage() {
  const { t } = useTranslation();
  const { flowId = '', token: tokenParam } = useParams();
  const [searchParams] = useSearchParams();
  const token = tokenParam ?? searchParams.get('token') ?? '';
  const hasValidFlowId = UUID_PATTERN.test(flowId);

  const [storedTicket, setStoredTicket] = useState<StoredQrTicket | null>(() =>
    hasValidFlowId ? getStoredQrTicket(flowId) : null
  );
  const [checking, setChecking] = useState(false);

  useEffect(() => {
    setStoredTicket(hasValidFlowId ? getStoredQrTicket(flowId) : null);
  }, [flowId, hasValidFlowId]);

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
      setStoredTicket(stored);
    },
  });

  const issueRef = useRef(issue.mutate);
  issueRef.current = issue.mutate;

  const autoIssueStarted = useRef(false);
  const newTicketAttempted = useRef(false);

  const isReload = useMemo(() => {
    const [entry] = performance.getEntriesByType('navigation') as PerformanceNavigationTiming[];
    return entry?.type === 'reload' || entry?.type === 'back_forward';
  }, []);

  useEffect(() => {
    newTicketAttempted.current = false;
  }, [token]);

  useEffect(() => {
    if (!hasValidFlowId || !token || storedTicket || autoIssueStarted.current) {
      return;
    }

    autoIssueStarted.current = true;
    issueRef.current();
  }, [hasValidFlowId, token, storedTicket]);

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

    if (token) {
      issueRef.current();
    }
  }, [flowId, statusQuery.error, storedTicket, token]);

  useEffect(() => {
    const status = statusQuery.data?.status;

    if (
      !storedTicket ||
      !token ||
      status === undefined ||
      !isFinishedTicketStatus(status) ||
      newTicketAttempted.current
    ) {
      return;
    }

    if (isReload) {
      return;
    }

    newTicketAttempted.current = true;
    issueRef.current();
  }, [isReload, storedTicket, token, statusQuery.data]);

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
  const isFinished = ticketStatus !== undefined && isFinishedTicketStatus(ticketStatus);
  const isInProgress = ticketStatus === QR_TICKET_STATUS.inProgress;
  const counterName = statusQuery.data?.counterName;
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
      <Paper withBorder radius="lg" p="xl" mt="xl">
        <Stack align="center" gap="md">
          {!hasValidFlowId ? (
            <Alert color="red" icon={<IconAlertTriangle size={18} />}>
              {t('qr.invalidLink')}
            </Alert>
          ) : storedTicket ? (
            statusPending ? (
              loadingView
            ) : isFinished ? (
              issue.isPending ? (
                loadingView
              ) : (
                <>
                  <ThemeIcon size={64} radius="xl" variant="light" color="gray">
                    <IconTicket size={36} />
                  </ThemeIcon>

                  <Text c="dimmed">
                    {storedTicket.flowName
                      ? t('qr.ticketFor', { name: storedTicket.flowName })
                      : t('qr.yourTurn')}
                  </Text>

                  <Title
                    order={1}
                    c="dimmed"
                    style={{ fontSize: 'clamp(3rem, 16vw, 6rem)', lineHeight: 1 }}
                  >
                    {storedTicket.code}
                  </Title>

                  <Title order={3} ta="center">
                    {t('qr.finishedTitle')}
                  </Title>
                  <Text ta="center" c="dimmed">
                    {t('qr.finishedMessage')}
                  </Text>
                </>
              )
            ) : (
              <>
                <ThemeIcon size={64} radius="xl" variant="light" color="teal">
                  <IconTicket size={36} />
                </ThemeIcon>

                <Text c="dimmed">
                  {storedTicket.flowName
                    ? t('qr.ticketFor', { name: storedTicket.flowName })
                    : t('qr.yourTurn')}
                </Text>

                <Title order={1} style={{ fontSize: 'clamp(4rem, 22vw, 9rem)', lineHeight: 1 }}>
                  {storedTicket.code}
                </Title>

                {isInProgress ? (
                  <>
                    <Title order={4} ta="center">
                      {t('qr.inProgressTitle')}
                    </Title>
                    <Text ta="center" c="dimmed">
                      {counterName
                        ? t('qr.inProgressAt', { counter: counterName })
                        : t('qr.inProgressNoCounter')}
                    </Text>
                  </>
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
          ) : (
            loadingView
          )}
        </Stack>
      </Paper>
    </Container>
  );
}
