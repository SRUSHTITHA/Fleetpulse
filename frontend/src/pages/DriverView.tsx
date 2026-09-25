import { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { getActiveTrip } from '../api/client';
import { useGeolocation, type GeoPermission } from '../hooks/useGeolocation';
import { StartTripForm } from '../components/driver/StartTripForm';
import { DriverTripCard } from '../components/driver/DriverTripCard';
import type { TripDto } from '../types';

function LocationRequired({ permission, requestLocation }: { permission: GeoPermission; requestLocation: () => void }) {
  const blocked = permission === 'denied' || permission === 'unsupported';
  return (
    <div className="driver-card gps-required-card">
      <div className="panel-header">
        <h2>Location access is required</h2>
      </div>
      <div className="gps-required-body">
        <p className="gps-required-strong">
          Sharing your location is mandatory to use FleetPulse — a trip cannot be started or tracked without it.
        </p>
        {blocked ? (
          <>
            <p>The browser is currently blocking location for this site. To allow it:</p>
            <ol>
              <li>Open the padlock or tune icon in the address bar</li>
              <li>Set Location to <strong>Allow</strong></li>
              <li>Come back here and tap Try again</li>
            </ol>
            <button type="button" className="btn-primary" onClick={requestLocation}>
              Try again
            </button>
          </>
        ) : (
          <>
            <p>
              Check the browser prompt and choose <strong>Allow</strong> so FleetPulse can get your position.
            </p>
            <button type="button" className="btn-primary" onClick={requestLocation}>
              Request location
            </button>
          </>
        )}
      </div>
    </div>
  );
}

export function DriverView() {
  const { logout } = useAuth();
  const [trip, setTrip] = useState<TripDto | null>(null);
  const [loading, setLoading] = useState(true);

  const geo = useGeolocation({
    enabled: true,
    intervalMs: 30000,
  });

  useEffect(() => {
    const refreshTrip = () => {
      void getActiveTrip()
        .then((t) => setTrip(t))
        .catch(() => setTrip(null))
        .finally(() => setLoading(false));
    };

    refreshTrip();
    const refreshTimer = window.setInterval(refreshTrip, 10000);
    return () => window.clearInterval(refreshTimer);
  }, []);

  const handleStarted = (t: TripDto) => setTrip(t);
  const handleEnded = () => setTrip(null);
  const locationGranted = geo.permission === 'granted' && geo.state != null;

  return (
    <div className="driver-page">
      <header className="dashboard-header">
        <div className="header-left">
          <div className="logo-mark">FP</div>
          <h1>FleetPulse · Driver</h1>
        </div>
        <div className="header-right">
          <button className="btn-ghost" onClick={logout}>
            Sign Out
          </button>
        </div>
      </header>

      <main className="driver-main">
        <div className="driver-column">
          {!locationGranted && (
            <LocationRequired permission={geo.permission} requestLocation={geo.requestLocation} />
          )}

          {loading ? (
            <div className="driver-card">
              <div className="empty-state">Loading your trip…</div>
            </div>
          ) : trip ? (
            <DriverTripCard trip={trip} location={geo.state} onEnded={handleEnded} />
          ) : (
            locationGranted && <StartTripForm onStarted={handleStarted} initialLocation={geo.state} />
          )}

          <div className="driver-gps-card">
            <div className="panel-header">
              <h2>GPS Status</h2>
            </div>
            <div className="gps-body">
              <div className={`gps-light ${geo.state ? 'on' : 'off'}`} />
              <div className="gps-info">
                {geo.state ? (
                  <>
                    <div className="gps-coords">
                      {geo.state.lat.toFixed(5)}, {geo.state.lng.toFixed(5)}
                    </div>
                    <div className="gps-speed">
                      {geo.state.speedKmh != null
                        ? `${geo.state.speedKmh.toFixed(0)} km/h`
                        : 'speed n/a'}
                    </div>
                  </>
                ) : (
                  <div className="gps-coords">{geo.error || 'Waiting for location permission…'}</div>
                )}
              </div>
              {!geo.state && (
                <button type="button" className="btn-small gps-retry" onClick={geo.requestLocation}>
                  Request location
                </button>
              )}
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
