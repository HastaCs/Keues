import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { useEffect, useState } from 'react';
import { buildMonitorHubUrl } from '@/features/flows/manualMonitorUrl';
import { getMonitorDeviceId } from './monitorDevice';
import { playBeep } from './sound';

export interface CalledTurn {
  code: string;
  counterCode: string;
  calledAt: number;
}

export type MonitorStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';

interface TicketCalledEvent {
  ticketCode?: string;
  counterCode?: string;
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api';
const MONITOR_NAME = 'Monitor web';

export function useMonitorConnection(locationId: string, flowId: string) {
  const [status, setStatus] = useState<MonitorStatus>('connecting');
  const [turns, setTurns] = useState<CalledTurn[]>([]);

  const enabled = Boolean(locationId && flowId);

  useEffect(() => {
    if (!enabled) {
      setStatus('disconnected');
      return;
    }

    let cancelled = false;
    setTurns([]);

    const hubUrl = buildMonitorHubUrl(window.location.href, API_BASE_URL, {
      deviceId: getMonitorDeviceId(),
      name: MONITOR_NAME,
      locationId,
      flowId,
    });

    const connection: HubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .build();

    connection.on('TicketCalled', (event: TicketCalledEvent) => {
      const ticketCode = event.ticketCode;
      if (cancelled || !ticketCode) {
        return;
      }

      playBeep();

      setTurns((current) => [
        { code: ticketCode, counterCode: event.counterCode ?? '—', calledAt: Date.now() },
        ...current.slice(0, 14),
      ]);
    });

    connection.onreconnecting(() => {
      if (!cancelled) {
        setStatus('reconnecting');
      }
    });
    connection.onreconnected(() => {
      if (!cancelled) {
        setStatus('connected');
      }
    });
    connection.onclose(() => {
      if (!cancelled) {
        setStatus('disconnected');
      }
    });

    setStatus('connecting');
    void connection
      .start()
      .then(() => {
        if (!cancelled) {
          setStatus('connected');
        }
      })
      .catch(() => {
        if (!cancelled) {
          setStatus('disconnected');
        }
      });

    return () => {
      cancelled = true;
      void connection.stop();
    };
  }, [enabled, locationId, flowId]);

  return { status, turns };
}
