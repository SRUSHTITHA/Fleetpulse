import { useEffect, useRef, useState } from 'react';
import { importLibrary, setOptions } from '@googlemaps/js-api-loader';
import { geocode, getDrivingRoute } from '../../api/client';
import type { RoutePoint } from '../../types';

interface GoogleRouteMapProps {
  origin: RoutePoint | null;
  destination: RoutePoint | string | null;
  destinationLabel?: string;
  onDestinationResolved?: (coords: RoutePoint) => void;
}

const googleMapsApiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY as string | undefined;

export function GoogleRouteMap({ origin, destination, destinationLabel, onDestinationResolved }: GoogleRouteMapProps) {
  const mapElement = useRef<HTMLDivElement>(null);
  const mapRef = useRef<google.maps.Map | null>(null);
  const routePolylineRef = useRef<google.maps.Polyline | null>(null);
  const destinationResolvedRef = useRef(onDestinationResolved);
  const [status, setStatus] = useState<'idle' | 'loading' | 'ready' | 'error'>('idle');
  const [errorMessage, setErrorMessage] = useState('');
  const originLat = origin?.lat ?? null;
  const originLng = origin?.lng ?? null;
  const destinationLat = typeof destination === 'string' ? null : destination?.lat ?? null;
  const destinationLng = typeof destination === 'string' ? null : destination?.lng ?? null;
  const destinationQuery = typeof destination === 'string' ? destination : null;
  const hasDestination = Boolean(destinationQuery) || (destinationLat !== null && destinationLng !== null);
  const [resolvedDestination, setResolvedDestination] = useState<RoutePoint | null>(
    typeof destination === 'string' ? null : destination,
  );

  useEffect(() => {
    destinationResolvedRef.current = onDestinationResolved;
  }, [onDestinationResolved]);

  useEffect(() => {
    if (originLat === null || originLng === null || !hasDestination || !mapElement.current || !googleMapsApiKey) return;

    let cancelled = false;
    const controller = new AbortController();
    setStatus('loading');
    setErrorMessage('');

    setOptions({ key: googleMapsApiKey, v: 'weekly' });
    void importLibrary('maps').then(async (mapsLibrary) => {
      if (cancelled || !mapElement.current) return;

      const originCoords = { lat: originLat, lng: originLng };
      const map = mapRef.current ?? new mapsLibrary.Map(mapElement.current, {
        center: originCoords,
        zoom: 12,
        mapTypeControl: false,
        streetViewControl: false,
        fullscreenControl: true,
      });
      mapRef.current = map;

      const destinationValue = destinationQuery ?? {
        lat: destinationLat!,
        lng: destinationLng!,
      };
      const destinationCoords = typeof destinationValue === 'string'
        ? await geocode(destinationValue)
        : destinationValue;
      if (cancelled || controller.signal.aborted) return;
      if (!destinationCoords) {
        setErrorMessage('Could not find that destination. Try a fuller address.');
        setStatus('error');
        return;
      }

      const coordinates = await getDrivingRoute(originCoords, destinationCoords, controller.signal);
      if (cancelled || controller.signal.aborted) return;
      if (coordinates.length < 2) {
        setErrorMessage('The routing service returned no driving route. Try a fuller destination address.');
        setStatus('error');
        return;
      }

      routePolylineRef.current?.setMap(null);
      routePolylineRef.current = new mapsLibrary.Polyline({
        path: coordinates.map(([lat, lng]) => ({ lat, lng })),
        map,
        strokeColor: '#df684c',
        strokeWeight: 5,
        strokeOpacity: 0.9,
      });

      const bounds = new google.maps.LatLngBounds();
      coordinates.forEach(([lat, lng]) => bounds.extend({ lat, lng }));
      map.fitBounds(bounds);
      const endpoint = coordinates[coordinates.length - 1];
      const resolvedCoords = { lat: endpoint[0], lng: endpoint[1] };
      setResolvedDestination(resolvedCoords);
      destinationResolvedRef.current?.(resolvedCoords);
      setStatus('ready');
    }).catch(() => {
      if (!cancelled && !controller.signal.aborted) {
        setErrorMessage('Google Maps could not load. Check the API key, billing, and localhost referrer restriction.');
        setStatus('error');
      }
    });

    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [originLat, originLng, hasDestination, destinationLat, destinationLng, destinationQuery]);

  const googleMapsUrl = origin && resolvedDestination
    ? `https://www.google.com/maps/dir/?api=1&origin=${origin.lat},${origin.lng}&destination=${resolvedDestination.lat},${resolvedDestination.lng}&travelmode=driving`
    : null;

  if (!googleMapsApiKey) {
    return <div className="google-map-message">Add key to show Google Maps here.</div>;
  }
  if (!origin) {
    return <div className="google-map-message">Waiting for GPS to plan the route.</div>;
  }
  if (!destination) {
    return <div className="google-map-message">Enter a destination to plan the route.</div>;
  }

  return (
    <div className="google-route-map-wrap">
      <div ref={mapElement} className="google-route-map" aria-label={`Route to ${destinationLabel ?? 'destination'}`} />
      <div className="google-map-status">
        {status === 'loading' && 'Planning route…'}
        {status === 'ready' && 'Route ready on Google Maps'}
        {status === 'error' && errorMessage}
      </div>
      {status === 'ready' && googleMapsUrl && (
        <a
          className="google-map-open-link"
          href={googleMapsUrl}
          target="_blank"
          rel="noreferrer"
        >
          Open route in Google Maps
        </a>
      )}
    </div>
  );
}
