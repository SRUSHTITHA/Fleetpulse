import { useEffect, useState } from 'react';
import { geocode } from '../../api/client';
import { GoogleRouteMap } from './GoogleRouteMap';
import type { RoutePoint } from '../../types';

interface RoutePreviewMapProps {
  originQuery: string;
  gpsOrigin: RoutePoint | null;
  destination: string;
  origin: RoutePoint | null;
  dest: RoutePoint | null;
  onOriginResolved?: (coords: RoutePoint | null) => void;
  onDestinationResolved?: (coords: RoutePoint | null) => void;
}

export function RoutePreviewMap({
  originQuery,
  gpsOrigin,
  destination,
  origin,
  dest,
  onOriginResolved,
  onDestinationResolved,
}: RoutePreviewMapProps) {
  const [status, setStatus] = useState<'idle' | 'geocoding' | 'resolved' | 'not-found'>('idle');

  useEffect(() => {
    let cancelled = false;
    const originText = originQuery.trim();
    const destText = destination.trim();

    const timer = window.setTimeout(async () => {
      if (!originText && !gpsOrigin && !destText) {
        if (cancelled) return;
        setStatus('idle');
        onOriginResolved?.(null);
        onDestinationResolved?.(null);
        return;
      }

      setStatus('geocoding');

      try {
        let originCoords = originText ? await geocode(originText) : gpsOrigin;
        if (cancelled) return;

        if (!originCoords && gpsOrigin) originCoords = gpsOrigin;

        onOriginResolved?.(originCoords);

        if (!destText) {
          onDestinationResolved?.(null);
          setStatus(originCoords ? 'resolved' : 'idle');
          return;
        }

        setStatus('resolved');
      } catch {
        if (cancelled) return;
        setStatus('not-found');
      }
    }, 450);

    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [originQuery, destination, gpsOrigin, onOriginResolved, onDestinationResolved]);

  const effectiveOrigin = origin ?? (!originQuery.trim() ? gpsOrigin : null);
  const hasInput = Boolean(effectiveOrigin || dest || originQuery.trim() || destination.trim() || gpsOrigin);

  return (
    <div className="route-preview-map">
      {!hasInput ? (
        <div className="map-empty-overlay">Enter an origin or destination to preview the route…</div>
      ) : (
        <GoogleRouteMap
          origin={effectiveOrigin}
          destination={dest ?? (destination.trim() || null)}
          destinationLabel={destination}
          onDestinationResolved={onDestinationResolved}
        />
      )}

      <div className="route-preview-status">
        {status === 'idle' && !dest ? (
          <span className="offline">● Waiting for origin or destination…</span>
        ) : status === 'geocoding' ? (
          <span className="offline">● Looking up addresses…</span>
        ) : status === 'not-found' ? (
          <span className="error2">● Could not find that address. Try a city or a fuller address.</span>
        ) : dest && origin ? (
          <span className="online">● Route planned </span>
        ) : dest ? (
          <span className="offline">● Destination found — now enter an origin or allow location for the route.</span>
        ) : origin ? (
          <span className="offline">● Origin ready — enter a destination.</span>
        ) : (
          <span className="offline">● Waiting for origin or destination…</span>
        )}
      </div>

    </div>
  );
}