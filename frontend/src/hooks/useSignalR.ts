import { useEffect, useRef } from 'react';
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';
import type { LocationBroadcastDto, ExceptionAlertDto } from '../types';

interface UseSignalRProps {
  token: string | null;
  onLocation?: (loc: LocationBroadcastDto) => void;
  onAlert?: (alert: ExceptionAlertDto) => void;
  onAlertRemoved?: (alertId: string) => void;
}

export function useSignalR({ token, onLocation, onAlert, onAlertRemoved }: UseSignalRProps) {
  const connRef = useRef<HubConnection | null>(null);
  const onLocationRef = useRef(onLocation);
  const onAlertRef = useRef(onAlert);
  const onAlertRemovedRef = useRef(onAlertRemoved);

  useEffect(() => {
    onLocationRef.current = onLocation;
    onAlertRef.current = onAlert;
    onAlertRemovedRef.current = onAlertRemoved;
  });

  useEffect(() => {
    if (!token) return;

    let cancelled = false;

    async function start() {
      const conn = new HubConnectionBuilder()
        .withUrl('/hubs/location', { accessTokenFactory: () => token! })
        .withAutomaticReconnect()
        .build();

      conn.on('BroadcastLocation', (loc: LocationBroadcastDto) => {
        onLocationRef.current?.(loc);
      });

      conn.on('SendAlert', (alert: ExceptionAlertDto) => {
        onAlertRef.current?.(alert);
      });

      conn.on('AlertRemoved', (alertId: string) => {
        onAlertRemovedRef.current?.(alertId);
      });

      try {
        await conn.start();
        if (!cancelled) {
          connRef.current = conn;
        } else {
          await conn.stop();
        }
      } catch {
        if (!cancelled) {
          setTimeout(start, 3000);
        }
      }
    }
    start();
    return () => {
      cancelled = true;
      connRef.current?.stop();
      connRef.current = null;
    };
  }, [token]);
}
