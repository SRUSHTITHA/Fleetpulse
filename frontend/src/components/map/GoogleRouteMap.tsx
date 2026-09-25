import { useEffect, useRef, useState } from 'react';
import { importLibrary, setOptions } from '@googlemaps/js-api-loader';
import { geocode } from '../../api/client';
import type { RoutePoint } from '../../types';

interface GoogleRouteMapProps {
  origin: RoutePoint | null;
  destination: RoutePoint | string | null;
  destinationLabel?: string;
  onDestinationResolved?: (coords: RoutePoint) => void;
}

interface OsrmRouteResponse {
  code: string;
  routes?: Array<{
    geometry?: {
      coordinates: [number, number][];
    };
  }>;
}

const googleMapsApiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY as string | undefined;

export function GoogleRouteMap({ origin, destination, destinationLabel, onDestinationResolved }: GoogleRouteMapProps) {
  const mapElement = useRef<HTMLDivElement>(null);
  const mapRef = useRef<google.maps.Map | null>(null);
  const routePolylineRef = useRef<google.maps.Polyline | null>(null);
  const destinationResolvedRef = useRef(onDestinationResolved);
  const [status, setStatus] = useState<'idle' | 'loading' | 'ready' | 'error'>('idle');
  const [errorMessage, setErrorMessage] = useState('');
  const destinationLat = typeof destination === 'string' ? null : destination?.lat ?? null;
  const destinationLng = typeof destination === 'string' ? null : destination?.lng ?? null;
  const destinationQuery = typeof destination === 'string' ? destination : null;
  const [resolvedDestination, setResolvedDestination] = useState<RoutePoint | null>(
    typeof destination === 'string' ? null : destination,
  );

  useEffect(() => {
    destinationResolvedRef.current = onDestinationResolved;
  }, [onDestinationResolved]);

  useEffect(() => {
    if (!origin || !destination || !mapElement.current || !googleMapsApiKey) return;

    let cancelled = false;
    setStatus('loading');
    setErrorMessage('');

    setOptions({ key: googleMapsApiKey, v: 'weekly' });
    void importLibrary('maps').then(async (mapsLibrary) => {
      if (cancelled || !mapElement.current) return;

      const map = mapRef.current ?? new mapsLibrary.Map(mapElement.current, {
        center: origin,
        zoom: 12,
        mapTypeControl: false,
        streetViewControl: false,
        fullscreenControl: true,
      });
      mapRef.current = map;

      const destinationCoords = typeof destination === 'string'
        ? await geocode(destination)
        : destination;
      if (cancelled) return;
      if (!destinationCoords) {
        setErrorMessage('Could not find that destination. Try a fuller address.');
        setStatus('error');
        return;
      }

      const routeUrl = new URL(
        `https://router.project-osrm.org/route/v1/driving/${origin.lng},${origin.lat};${destinationCoords.lng},${destinationCoords.lat}`,
      );
      routeUrl.searchParams.set('overview', 'full');
      routeUrl.searchParams.set('geometries', 'geojson');
      const response = await fetch(routeUrl);
      const routeData = (await response.json()) as OsrmRouteResponse;
      if (cancelled) return;

      const coordinates = routeData.routes?.[0]?.geometry?.coordinates;
      if (!response.ok || routeData.code !== 'Ok' || !coordinates?.length) {
        setErrorMessage('OSRM returned no driving route. Try a fuller destination address.');
        setStatus('error');
        return;
      }

      routePolylineRef.current?.setMap(null);
      routePolylineRef.current = new mapsLibrary.Polyline({
        path: coordinates.map(([lng, lat]) => ({ lat, lng })),
        map,
        strokeColor: '#df684c',
        strokeWeight: 5,
        strokeOpacity: 0.9,
      });

      const bounds = new google.maps.LatLngBounds();
      coordinates.forEach(([lng, lat]) => bounds.extend({ lat, lng }));
      map.fitBounds(bounds);
      const endpoint = coordinates[coordinates.length - 1];
      const resolvedCoords = { lat: endpoint[1], lng: endpoint[0] };
      setResolvedDestination(resolvedCoords);
      destinationResolvedRef.current?.(resolvedCoords);
      setStatus('ready');
    }).catch(() => {
      if (!cancelled) {
        setErrorMessage('Google Maps could not load. Check the API key, billing, and localhost referrer restriction.');
        setStatus('error');
      }
    });

    return () => {
      cancelled = true;
    };
  }, [origin?.lat, origin?.lng, destinationLat, destinationLng, destinationQuery]);

  const googleMapsUrl = origin && resolvedDestination
    ? `https://www.google.com/maps/dir/?api=1&origin=${origin.lat},${origin.lng}&destination=${resolvedDestination.lat},${resolvedDestination.lng}&travelmode=driving`
    : null;

  if (!googleMapsApiKey) {
    return <div className="google-map-message">Add <code>VITE_GOOGLE_MAPS_API_KEY</code> to show Google Maps here.</div>;
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
        {status === 'ready' && 'OSRM route ready on Google Maps'}
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