import { useEffect, useRef, useState } from 'react';
import { MapContainer, Marker, Popup, Polyline, CircleMarker, Tooltip, TileLayer, useMap } from 'react-leaflet';
import L from 'leaflet';
import type { MapMarker, TripDto } from '../../types';
import { getDrivingRoute } from '../../api/client';

function createVehicleIcon(isSelected: boolean): L.DivIcon {
  return L.divIcon({
    className: 'vehicle-marker',
    html: `<div class="marker-dot ${isSelected ? 'marker-alert' : 'marker-normal'}"></div>`,
    iconSize: [16, 16],
    iconAnchor: [8, 8],
  });
}

interface MapUpdaterProps {
  markers: MapMarker[];
  trips: TripDto[];
  selectedKey: string | null;
}

function MapUpdater({ markers, trips, selectedKey }: MapUpdaterProps) {
  const map = useMap();
  const hasFlownRef = useRef(false);
  const lastBoundsKey = useRef('');

  useEffect(() => {
    const activeTrips = trips.filter((trip) => trip.status === 'InProgress');
    if (!markers.length && !activeTrips.length) return;

    const boundsKey = [
      ...markers.map((m) => `${m.key}:${m.latitude}:${m.longitude}`),
      ...activeTrips.map((trip) => `${trip.id}:${trip.originLat}:${trip.originLng}:${trip.destLat}:${trip.destLng}`),
    ].join('|');
    if (boundsKey === lastBoundsKey.current) return;

    lastBoundsKey.current = boundsKey;
    const points: [number, number][] = markers.map((m) => [m.latitude, m.longitude]);
    for (const trip of activeTrips) {
      points.push([trip.originLat, trip.originLng], [trip.destLat, trip.destLng]);
    }
    const bounds = L.latLngBounds(points);
    map.fitBounds(bounds, { padding: [40, 40], maxZoom: 13 });
    hasFlownRef.current = false;
  }, [markers, trips, map]);

  useEffect(() => {
    const selected = markers.find((m) => m.key === selectedKey);
    const selectedTrip = trips.find((trip) => `trip-${trip.id}` === selectedKey);
    if (selectedTrip) {
      const bounds = L.latLngBounds([
        [selectedTrip.originLat, selectedTrip.originLng],
        [selectedTrip.destLat, selectedTrip.destLng],
      ]);
      map.fitBounds(bounds, { padding: [60, 60], maxZoom: 13 });
      hasFlownRef.current = true;
      return;
    }
    if (selected) {
      map.flyTo([selected.latitude, selected.longitude], Math.max(map.getZoom(), 13), {
        duration: 0.8,
      });
      hasFlownRef.current = true;
    } else if (!hasFlownRef.current && !markers.length) {
      map.setView([40.7128, -74.006], 11);
      hasFlownRef.current = true;
    }
  }, [selectedKey, markers, trips, map]);

  return null;
}

interface FleetMapProps {
  markers: MapMarker[];
  trips: TripDto[];
  selectedKey: string | null;
  onSelectMarker: (key: string) => void;
}

export function FleetMap({ markers, trips, selectedKey, onSelectMarker }: FleetMapProps) {
  const activeTrips = trips.filter((trip) => trip.status === 'InProgress');
  const [roadRoutes, setRoadRoutes] = useState<Record<string, [number, number][]>>({});

  useEffect(() => {
    const controller = new AbortController();

    async function loadRoadRoutes() {
      const results = await Promise.all(
        activeTrips.map(async (trip) => {
          try {
            const route = await getDrivingRoute(
              { lat: trip.originLat, lng: trip.originLng },
              { lat: trip.destLat, lng: trip.destLng },
            );
            if (route.length < 2) return [trip.id, null] as const;
            return [trip.id, route] as const;
          } catch {
            return [trip.id, null] as const;
          }
        }),
      );

      if (controller.signal.aborted) return;
      setRoadRoutes(Object.fromEntries(results.filter(([, route]) => route)) as Record<string, [number, number][]>);
    }

    void loadRoadRoutes();
    return () => controller.abort();
  }, [trips]);

  return (
    <MapContainer
      className="fleet-map"
      center={[40.7128, -74.006]}
      zoom={11}
      zoomControl={false}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <MapUpdater markers={markers} trips={trips} selectedKey={selectedKey} />
      {markers.length === 0 && activeTrips.length === 0 && (
        <div className="map-empty-overlay">No active vehicles or planned routes yet</div>
      )}
      {activeTrips.map((trip) => {
        const selected = selectedKey === `trip-${trip.id}`;
        const route = roadRoutes[trip.id] ?? [
          [trip.originLat, trip.originLng],
          [trip.destLat, trip.destLng],
        ] as [number, number][];

        return (
          <>
            <Polyline
              key={`route-outline-${trip.id}`}
              positions={route}
              pathOptions={{
                color: '#fff8f2',
                weight: selected ? 9 : 7,
                opacity: 0.9,
              }}
            />
            <Polyline
              key={`route-${trip.id}`}
              positions={route}
              eventHandlers={{ click: () => onSelectMarker(`trip-${trip.id}`) }}
              pathOptions={{
                color: selected ? '#b93828' : '#e05236',
                weight: selected ? 5 : 4,
                opacity: 1,
              }}
            >
              <Tooltip sticky>Planned route · {trip.origin} → {trip.destination}</Tooltip>
            </Polyline>
          </>
        );
      })}
      {activeTrips.map((trip) => (
        <CircleMarker
          key={`origin-${trip.id}`}
          center={[trip.originLat, trip.originLng]}
          radius={7}
          pathOptions={{ color: '#087f5b', fillColor: '#2dd4a0', fillOpacity: 1, weight: 3 }}
          eventHandlers={{ click: () => onSelectMarker(`trip-${trip.id}`) }}
        >
          <Tooltip>Origin · {trip.origin}</Tooltip>
        </CircleMarker>
      ))}
      {activeTrips.map((trip) => (
        <CircleMarker
          key={`destination-${trip.id}`}
          center={[trip.destLat, trip.destLng]}
          radius={7}
          pathOptions={{ color: '#b4233d', fillColor: '#ff6b81', fillOpacity: 1, weight: 3 }}
          eventHandlers={{ click: () => onSelectMarker(`trip-${trip.id}`) }}
        >
          <Tooltip>Destination · {trip.destination}</Tooltip>
        </CircleMarker>
      ))}
      {activeTrips.length > 0 && (
        <div className="map-route-legend">
          <span><i className="legend-origin" /> Origin</span>
          <span><i className="legend-route" /> Planned route</span>
          <span><i className="legend-destination" /> Destination</span>
        </div>
      )}
      {markers.map((marker) => {
        const isSelected = marker.key === selectedKey;
        const icon = createVehicleIcon(isSelected);
        return (
          <Marker
            key={marker.key}
            position={[marker.latitude, marker.longitude]}
            icon={icon}
            eventHandlers={{
              click: () => onSelectMarker(marker.key),
            }}
          >
            <Popup>
              <div className="marker-popup">
                <strong>{marker.vehicleName}</strong>
                <span className="marker-plate">{marker.licensePlate}</span>
                <span>{marker.driverName || 'Unassigned'}</span>
                {marker.speedKmh != null && (
                  <span>{marker.speedKmh.toFixed(0)} km/h</span>
                )}
              </div>
            </Popup>
          </Marker>
        );
      })}
    </MapContainer>
  );
}
