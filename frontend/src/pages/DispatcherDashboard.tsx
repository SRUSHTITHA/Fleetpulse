import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useSignalR } from '../hooks/useSignalR';
import { getTrips, getExceptions, getVehicles, getDrivers, createVehicle, updateVehicle, deleteVehicle, deleteException } from '../api/client';
import { FleetMap } from '../components/map/FleetMap';
import { VehicleList } from '../components/dashboard/VehicleList';
import { VehicleForm } from '../components/dashboard/VehicleForm';
import { AlertPanel } from '../components/dashboard/AlertPanel';
import { AlertDetail } from '../components/dashboard/AlertDetail';
import type { TripDto, LocationBroadcastDto, ExceptionAlertDto, MapMarker, VehicleDto, DriverDto, VehicleInput } from '../types';
import { isTripInProgress } from '../utils/helpers';

export function DispatcherDashboard() {
  const { token, user, logout } = useAuth();
  const [trips, setTrips] = useState<TripDto[]>([]);
  const [vehicles, setVehicles] = useState<VehicleDto[]>([]);
  const [drivers, setDrivers] = useState<DriverDto[]>([]);
  const [locations, setLocations] = useState<Map<string, LocationBroadcastDto>>(new Map());
  const [alerts, setAlerts] = useState<ExceptionAlertDto[]>([]);
  const [selectedKey, setSelectedKey] = useState<string | null>(null);
  const [showVehicleForm, setShowVehicleForm] = useState(false);
  const [selectedAlert, setSelectedAlert] = useState<ExceptionAlertDto | null>(null);
  const [pageError, setPageError] = useState('');

  const mergeTripsIntoLocations = useCallback((ts: TripDto[]) => {
    setLocations((prev) => {
      const next = new Map(prev);
      const activeTripIds = new Set(
        ts.filter((trip) => isTripInProgress(trip)).map((trip) => trip.id),
      );

      for (const tripId of next.keys()) {
        if (!activeTripIds.has(tripId)) next.delete(tripId);
      }

      for (const t of ts) {
        if (isTripInProgress(t) && t.lastLatitude != null && t.lastLongitude != null) {
          next.set(t.id, {
            tripId: t.id,
            driverId: t.driverId,
            driverName: t.driverName ?? '',
            vehicleId: t.vehicleId,
            vehicleName: t.vehicleName ?? '',
            licensePlate: t.licensePlate ?? '',
            latitude: t.lastLatitude,
            longitude: t.lastLongitude,
            speedKmh: t.lastSpeedKmh,
            recordedAt: t.lastRecordedAt ?? new Date().toISOString(),
          });
        }
      }
      return next;
    });
  }, []);

  const reloadTrips = useCallback(async () => {
    try {
      const ts = await getTrips();
      setTrips(ts);
      mergeTripsIntoLocations(ts);
    } catch {
      setPageError('Failed to load trips.');
    }
  }, [mergeTripsIntoLocations]);

  const reloadVehicles = useCallback(async () => {
    try {
      setVehicles(await getVehicles());
    } catch {
      setPageError('Failed to load vehicles.');
    }
  }, []);

  const reloadDrivers = useCallback(async () => {
    try {
      setDrivers(await getDrivers());
    } catch {
      setPageError('Failed to load drivers.');
    }
  }, []);

  useEffect(() => {
    getExceptions().then(setAlerts).catch(() => {});
    getTrips()
      .then((ts) => {
        setTrips(ts);
        mergeTripsIntoLocations(ts);
      })
      .catch(() => setPageError('Failed to load trips.'));
    getVehicles().then(setVehicles).catch(() => setPageError('Failed to load vehicles.'));
    getDrivers().then(setDrivers).catch(() => setPageError('Failed to load drivers.'));

    const refreshTimer = window.setInterval(() => {
      void reloadTrips();
      void reloadVehicles();
    }, 10000);

    return () => window.clearInterval(refreshTimer);
  }, [mergeTripsIntoLocations, reloadTrips, reloadVehicles]);

  const handleLocation = useCallback((loc: LocationBroadcastDto) => {
    setLocations((prev) => {
      const next = new Map(prev);
      next.set(loc.tripId, loc);
      return next;
    });
    void reloadTrips();
  }, [reloadTrips]);

  const handleAlert = useCallback((alert: ExceptionAlertDto) => {
    setAlerts((prev) => (prev.some((a) => a.id === alert.id) ? prev : [alert, ...prev].slice(0, 50)));
  }, []);

  const handleAlertRemoved = useCallback((alertId: string) => {
    setAlerts((prev) => prev.filter((a) => a.id !== alertId));
    setSelectedAlert((sel) => (sel?.id === alertId ? null : sel));
  }, []);

  useSignalR({ token, onLocation: handleLocation, onAlert: handleAlert, onAlertRemoved: handleAlertRemoved });

  const markers = useMemo<MapMarker[]>(() => {
    const byKey = new Map<string, MapMarker>();

    for (const loc of locations.values()) {
      byKey.set(`trip-${loc.tripId}`, {
        key: `trip-${loc.tripId}`,
        tripId: loc.tripId,
        vehicleId: loc.vehicleId,
        vehicleName: loc.vehicleName,
        licensePlate: loc.licensePlate,
        driverName: loc.driverName,
        speedKmh: loc.speedKmh,
        latitude: loc.latitude,
        longitude: loc.longitude,
        recordedAt: loc.recordedAt,
        source: 'trip',
      });
    }

    for (const t of trips.filter((trip) => isTripInProgress(trip))) {
      if (byKey.has(`trip-${t.id}`)) continue;

      const vehicle = vehicles.find((candidate) => candidate.id === t.vehicleId);
      const latitude = t.lastLatitude ?? vehicle?.lastLatitude ?? vehicle?.latitude;
      const longitude = t.lastLongitude ?? vehicle?.lastLongitude ?? vehicle?.longitude;
      if (latitude == null || longitude == null) continue;

      byKey.set(`trip-${t.id}`, {
        key: `trip-${t.id}`,
        tripId: t.id,
        vehicleId: t.vehicleId,
        vehicleName: t.vehicleName ?? '',
        licensePlate: t.licensePlate ?? '',
        driverName: t.driverName ?? '',
        speedKmh: t.lastSpeedKmh,
        latitude,
        longitude,
        recordedAt: t.lastRecordedAt ?? new Date().toISOString(),
        source: 'trip',
      });
    }

    const tripVehicleIds = new Set<string>();
    for (const m of byKey.values()) tripVehicleIds.add(m.vehicleId);

    for (const v of vehicles) {
      if (v.lastLatitude == null || v.lastLongitude == null) continue;
      if (tripVehicleIds.has(v.id)) continue;
      byKey.set(`veh-${v.id}`, {
        key: `veh-${v.id}`,
        vehicleId: v.id,
        vehicleName: v.name,
        licensePlate: v.licensePlate,
        driverName: v.assignedDriverName ?? '',
        speedKmh: v.lastSpeedKmh,
        latitude: v.lastLatitude,
        longitude: v.lastLongitude,
        recordedAt: v.lastRecordedAt ?? new Date().toISOString(),
        source: 'vehicle',
      });
    }

    return [...byKey.values()];
  }, [locations, trips, vehicles]);

  const selectedMarker = selectedKey ? markers.find((m) => m.key === selectedKey) : undefined;

  const handleCreateVehicle = async (data: VehicleInput) => {
    await createVehicle(data);
    setShowVehicleForm(false);
    await Promise.all([reloadVehicles(), reloadTrips(), reloadDrivers()]);
  };

  const handleUpdateVehicle = async (id: string, req: VehicleInput) => {
    await updateVehicle(id, req);
    await Promise.all([reloadVehicles(), reloadTrips(), reloadDrivers()]);
  };

  const handleDeleteVehicle = async (id: string) => {
    if (!window.confirm('Remove this vehicle? Any active trip using it will be cancelled.')) return;
    try {
      await deleteVehicle(id);
      setSelectedKey((key) => (key === `veh-${id}` ? null : key));
      setVehicles((current) => current.filter((vehicle) => vehicle.id !== id));
      await Promise.all([reloadVehicles(), reloadTrips(), reloadDrivers()]);
    } catch {
      setPageError('Failed to delete vehicle.');
    }
  };

  const handleDeleteAlert = async (alert: ExceptionAlertDto) => {
    if (!window.confirm('Permanently delete this alert?')) return;
    try {
      await deleteException(alert.id);
      setAlerts((prev) => prev.filter((a) => a.id !== alert.id));
      setSelectedAlert((sel) => (sel?.id === alert.id ? null : sel));
    } catch {
      setPageError('Failed to delete alert.');
    }
  };

  const handleLocateAlert = (tripId: string) => {
    setSelectedAlert(null);
    setSelectedKey(`trip-${tripId}`);
  };

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <div className="header-left">
          <div className="logo-mark">FP</div>
          <div>
            <h1>FleetPulse</h1>
          </div>
        </div>
        <div className="header-right">
          <div className="operations-summary">
            <span className="live-indicator"><i /> Live</span>
            <span>{trips.filter((trip) => isTripInProgress(trip)).length} active trips</span>
            <span>{alerts.length} alerts</span>
          </div>
          {user?.role === 'Admin' && (
            <Link to="/admin" className="btn-nav" role="button">
              Settings
            </Link>
          )}
          <button className="btn-ghost" onClick={logout}>
            Sign Out
          </button>
        </div>
      </header>

      {pageError && <div className="dashboard-error">{pageError}</div>}

      <div className="dashboard-body">
        <aside className="sidebar">
          <VehicleList
            vehicles={vehicles}
            drivers={drivers}
            selectedKey={selectedKey}
            onSelectVehicle={(key) => {
              const vehicleId = key.replace('veh-', '');
              const activeTrip = trips.find(
                (trip) => isTripInProgress(trip) && trip.vehicleId === vehicleId,
              );
              setSelectedKey(activeTrip ? `trip-${activeTrip.id}` : key);
            }}
            onAddVehicle={() => setShowVehicleForm(true)}
            onUpdateVehicle={handleUpdateVehicle}
            onDeleteVehicle={(id) => void handleDeleteVehicle(id)}
          />
        </aside>

        <main className="map-area">
          <FleetMap
            markers={markers}
            trips={trips}
            selectedKey={selectedKey}
            onSelectMarker={setSelectedKey}
          />
        </main>

        <aside className="alerts-sidebar">
          <AlertPanel alerts={alerts} onOpen={setSelectedAlert} onDelete={(a) => void handleDeleteAlert(a)} />
        </aside>
      </div>

      {showVehicleForm && (
        <VehicleForm
          drivers={drivers}
          onSave={handleCreateVehicle}
          onCancel={() => setShowVehicleForm(false)}
        />
      )}

      {selectedAlert && (
        <AlertDetail
          alert={selectedAlert}
          onLocate={handleLocateAlert}
          onDelete={(a) => void handleDeleteAlert(a)}
          onClose={() => setSelectedAlert(null)}
        />
      )}

      {selectedMarker && (
        <div className="map-hint">
          {selectedMarker.vehicleName} · {selectedMarker.licensePlate}
        </div>
      )}
    </div>
  );
}