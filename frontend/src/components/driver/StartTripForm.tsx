import { useState } from 'react';
import { startTrip } from '../../api/client';
import type { TripDto } from '../../types';
import type { GeolocationState } from '../../hooks/useGeolocation';
import { RoutePreviewMap } from '../map/RoutePreviewMap';

interface StartTripFormProps {
  onStarted: (trip: TripDto) => void;
  initialLocation: GeolocationState | null;
}

function toLocalInputValue(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function StartTripForm({ onStarted, initialLocation }: StartTripFormProps) {
  const [origin, setOrigin] = useState('');
  const [destination, setDestination] = useState('');
  const [eta, setEta] = useState('');
  const [originCoords, setOriginCoords] = useState<{ lat: number; lng: number } | null>(null);
  const [destCoords, setDestCoords] = useState<{ lat: number; lng: number } | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const minEta = toLocalInputValue(new Date());
  const gps = initialLocation ? { lat: initialLocation.lat, lng: initialLocation.lng } : null;
  const resolvedOrigin = originCoords ?? gps;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    if (!gps) {
      setError('Location permission is required before you can start a trip.');
      return;
    }
    if (!destination.trim()) {
      setError('Destination is required.');
      return;
    }
    if (!resolvedOrigin) {
      setError('Enter an origin address or allow GPS so we can place the start of the trip.');
      return;
    }
    if (!destCoords) {
      setError('Could not find that destination. Try a city name or a fuller address.');
      return;
    }
    if (!eta) {
      setError('Estimated arrival time is required.');
      return;
    }

    const arrival = new Date(eta);
    if (Number.isNaN(arrival.getTime())) {
      setError('Estimated arrival time is invalid.');
      return;
    }
    if (arrival.getTime() <= Date.now()) {
      setError('Estimated arrival must be in the future.');
      return;
    }

    setLoading(true);
    try {
      const trip = await startTrip({
        origin: origin.trim() || 'Current location',
        destination: destination.trim(),
        originLat: resolvedOrigin.lat,
        originLng: resolvedOrigin.lng,
        destLat: destCoords.lat,
        destLng: destCoords.lng,
        estimatedArrival: arrival.toISOString(),
      });
      onStarted(trip);
    } catch {
      setError('Could not start trip. You may already have an active trip or no vehicle assigned.');
    } finally {
      setLoading(false);
    }
  };

  const canStart = Boolean(gps) && Boolean(destCoords) && !loading;

  return (
    <div className="driver-card">
      <div className="panel-header">
        <h2>Start a Trip</h2>
      </div>
      <form onSubmit={handleSubmit} className="driver-form">
        {error && <div className="driver-error">{error}</div>}
        <div className="form-group">
          <label htmlFor="origin">Origin</label>
          <input
            id="origin"
            type="text"
            value={origin}
            onChange={(e) => setOrigin(e.target.value)}
            placeholder="City or address (or leave blank to use GPS)"
            autoComplete="off"
          />
        </div>
        <div className="form-group">
          <label htmlFor="destination">Destination</label>
          <input
            id="destination"
            type="text"
            value={destination}
            onChange={(e) => setDestination(e.target.value)}
            placeholder="e.g. Bidar, Karnataka"
            required
            autoComplete="off"
          />
        </div>

        <RoutePreviewMap
          originQuery={origin}
          gpsOrigin={gps}
          destination={destination}
          origin={originCoords}
          dest={destCoords}
          onOriginResolved={setOriginCoords}
          onDestinationResolved={setDestCoords}
        />

        <div className="form-group">
          <label htmlFor="eta">Estimated Arrival</label>
          <input
            id="eta"
            type="datetime-local"
            min={minEta}
            value={eta}
            onChange={(e) => setEta(e.target.value)}
            required
          />
        </div>
        <div className="location-hint">
          {gps ? (
            <span className="online">
              ● GPS locked ({gps.lat.toFixed(5)}, {gps.lng.toFixed(5)})
            </span>
          ) : (
            <span className="offline">● Location is required — allow GPS to start a trip.</span>
          )}
        </div>
        <button type="submit" className="btn-primary" disabled={!canStart}>
          {loading ? 'Starting…' : 'Start Trip'}
        </button>
      </form>
    </div>
  );
}
