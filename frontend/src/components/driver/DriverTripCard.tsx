import { useEffect, useState, useRef } from 'react';
import { endTrip, cancelTrip, reportLocation } from '../../api/client';
import type { TripDto } from '../../types';
import { formatDateTime } from '../../utils/helpers';
import type { GeolocationState } from '../../hooks/useGeolocation';
import { GoogleRouteMap } from '../map/GoogleRouteMap';

interface DriverTripCardProps {
  trip: TripDto;
  location: GeolocationState | null;
  onEnded: () => void;
}

export function DriverTripCard({ trip, location, onEnded }: DriverTripCardProps) {
  const [lastSent, setLastSent] = useState<string | undefined>();
  const [error, setError] = useState('');
  const [elapsed, setElapsed] = useState(0);
  const initialSentRef = useRef(false);

  // Elapsed timer from actual departure
  useEffect(() => {
    const start = trip.actualDeparture ? new Date(trip.actualDeparture).getTime() : Date.now();
    const timer = window.setInterval(() => {
      setElapsed(Math.floor((Date.now() - start) / 1000));
    }, 1000);
    return () => window.clearInterval(timer);
  }, [trip.actualDeparture]);

  // Report a single location. The driver's geolocation hook updates `location`
  // roughly every 30 s, so each pop of that state triggers one POST here.
  useEffect(() => {
    if (!location) return;

    // Fire once immediately so we pick up the current fix without waiting a full cycle.
    if (!initialSentRef.current) {
      initialSentRef.current = true;
    }

    const send = async () => {
      try {
        await reportLocation({
          tripId: trip.id,
          latitude: location.lat,
          longitude: location.lng,
          speedKmh: location.speedKmh,
        });
        setError('');
        setLastSent(new Date().toISOString());
      } catch {
        setError('Failed to send location. Retrying on next update.');
      }
    };
    void send();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location?.lat, location?.lng, location?.speedKmh]);

  const fmt = (s: number) =>
    `${String(Math.floor(s / 3600)).padStart(2, '0')}:${String(
      Math.floor((s % 3600) / 60),
    ).padStart(2, '0')}:${String(s % 60).padStart(2, '0')}`;

  const handleCancel = async () => {
    setError('');
    try {
      await cancelTrip(trip.id);
      onEnded();
    } catch {
      setError('Failed to cancel trip.');
    }
  };

  const handleEnd = async () => {
    setError('');
    try {
      await endTrip(trip.id);
      onEnded();
    } catch {
      setError('Failed to end trip.');
    }
  };

  return (
    <div className="driver-card active-trip">
      <div className="panel-header">
        <h2>Active Trip</h2>
        <span className="status-chip inprogress">In Progress</span>
      </div>

      <div className="trip-details">
        <div className="trip-route">
          <div className="route-line">
            <span className="route-dot origin" />
            <span>{trip.origin}</span>
          </div>
          <div className="route-line">
            <span className="route-dot dest" />
            <span>{trip.destination}</span>
          </div>
        </div>
        <dl className="trip-stats">
          <div>
            <dt>Vehicle</dt>
            <dd>
              {trip.vehicleName} <span className="plate">({trip.licensePlate})</span>
            </dd>
          </div>
          <div>
            <dt>Elapsed</dt>
            <dd className="mono">{fmt(elapsed)}</dd>
          </div>
          <div>
            <dt>ETA</dt>
            <dd>{trip.estimatedArrival ? formatDateTime(trip.estimatedArrival) : '—'}</dd>
          </div>
          <div>
            <dt>Last sent</dt>
            <dd>{lastSent ? formatDateTime(lastSent) : '—'}</dd>
          </div>
        </dl>
      </div>

      <div className="location-status">
        {location ? (
          <span className="online">
            ● GPS live {location.speedKmh != null ? `· ${location.speedKmh.toFixed(0)} km/h` : ''}
          </span>
        ) : (
          <span className="offline">● No GPS — location will not send</span>
        )}
      </div>

      <div className="active-trip-map">
        <GoogleRouteMap
          origin={{ lat: trip.originLat, lng: trip.originLng }}
          destination={{ lat: trip.destLat, lng: trip.destLng }}
          destinationLabel={trip.destination}
        />
      </div>

      {error && <div className="driver-error">{error}</div>}

      <div className="driver-actions">
        <button className="btn-danger" onClick={handleEnd}>
          End Trip
        </button>
        <button className="btn-ghost" onClick={handleCancel}>
          Cancel
        </button>
      </div>
    </div>
  );
}
