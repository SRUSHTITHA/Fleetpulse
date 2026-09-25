import { useState } from 'react';
import type { DriverDto, VehicleDto, VehicleInput } from '../../types';
import { formatTime } from '../../utils/helpers';

interface VehicleListProps {
  vehicles: VehicleDto[];
  drivers: DriverDto[];
  selectedKey: string | null;
  onSelectVehicle: (key: string) => void;
  onAddVehicle: () => void;
  onUpdateVehicle: (id: string, req: VehicleInput) => Promise<void>;
  onDeleteVehicle: (id: string) => void;
}

function toVehicleInput(vehicle: VehicleDto, overrides: Partial<VehicleInput> = {}): VehicleInput {
  return {
    name: vehicle.name,
    licensePlate: vehicle.licensePlate,
    type: vehicle.type,
    isActive: vehicle.isActive,
    driverId: vehicle.assignedDriverId ?? null,
    latitude: vehicle.latitude ?? null,
    longitude: vehicle.longitude ?? null,
    ...overrides,
  };
}

export function VehicleList({ vehicles, drivers, selectedKey, onSelectVehicle, onAddVehicle, onUpdateVehicle, onDeleteVehicle }: VehicleListProps) {
  const [showInactive, setShowInactive] = useState(false);
  const activeCount = vehicles.filter((v) => v.isActive).length;
  const visible = showInactive ? vehicles : vehicles.filter((v) => v.isActive);

  const handleAssign = async (vehicle: VehicleDto, driverId: string) => {
    if (driverId === (vehicle.assignedDriverId ?? '')) return;
    await onUpdateVehicle(vehicle.id, toVehicleInput(vehicle, { driverId: driverId || null }));
  };

  const handleToggleActive = async (vehicle: VehicleDto) => {
    await onUpdateVehicle(vehicle.id, toVehicleInput(vehicle, { isActive: !vehicle.isActive }));
  };

  return (
    <div className="vehicle-list">
      <div className="panel-header">
        <h2>Vehicles</h2>
        <span className="panel-actions">
          <button
            className={`filter-toggle ${showInactive ? 'on' : ''}`}
            onClick={() => setShowInactive((v) => !v)}
            title="Show inactive vehicles"
          >
            {showInactive ? 'Active + Inactive' : 'Active'}
          </button>
          <button className="btn-add" onClick={onAddVehicle} title="Add vehicle">
            + Add
          </button>
        </span>
      </div>
      <div className="vehicle-count-row">
        <span className="count-badge">{activeCount} active vehicles</span>
        {showInactive && (
          <span className="count-badge muted">{vehicles.length - activeCount} inactive</span>
        )}
      </div>
      <div className="vehicle-items">
        {visible.length === 0 && (
          <div className="empty-state">
            {showInactive
              ? 'No vehicles registered yet. Add one above.'
              : 'No active vehicles. Toggle "Active + Inactive" or add a vehicle.'}
          </div>
        )}
        {visible.map((vehicle) => {
          const hasPosition =
            vehicle.lastLatitude != null && vehicle.lastLongitude != null;
          const key = `veh-${vehicle.id}`;
          const isSelected = selectedKey === key;
          return (
            <div
              key={vehicle.id}
              className={`vehicle-item ${isSelected ? 'selected' : ''} ${vehicle.isActive ? '' : 'vehicle-inactive'}`}
              onClick={() => {
                onSelectVehicle(key);
              }}
              title={hasPosition ? 'Show on map' : 'No location yet; select to view details'}
            >
              <div className="vehicle-icon">
                <div className={`dot ${vehicle.isActive ? 'online' : 'offline'}`} />
              </div>
              <div className="vehicle-info">
                <div className="vehicle-name">
                  {vehicle.name}
                  <span className="vehicle-type">{vehicle.type}</span>
                </div>
                <div className="vehicle-meta">
                  <span className="plate">{vehicle.licensePlate}</span>
                  <span className="divider">·</span>
                  <span>{vehicle.assignedDriverName || 'Unassigned'}</span>
                </div>
                <div className="vehicle-assign">
                  <select
                    className="assign-select"
                    value={vehicle.assignedDriverId ?? ''}
                    onClick={(e) => e.stopPropagation()}
                    onChange={(e) => void handleAssign(vehicle, e.target.value)}
                  >
                    <option value="">Unassigned</option>
                    {drivers.map((d) => (
                      <option key={d.id} value={d.id}>
                        {d.fullName} {d.vehicleId ? '(has vehicle)' : ''}
                      </option>
                    ))}
                  </select>
                </div>
                {hasPosition && (
                  <div className="vehicle-speed">
                    {vehicle.lastSpeedKmh != null
                      ? `${vehicle.lastSpeedKmh.toFixed(0)} km/h`
                      : 'Idle'}
                    <span className="time-sep">·</span>
                    {formatTime(vehicle.lastRecordedAt!)}
                  </div>
                )}
              </div>
              <div className="vehicle-side">
                <button
                  className="vehicle-delete-btn"
                  onClick={(e) => {
                    e.stopPropagation();
                    onDeleteVehicle(vehicle.id);
                  }}
                  title="Delete vehicle"
                  aria-label={`Delete ${vehicle.name}`}
                >
                  x
                </button>
                <button
                  className={`toggle ${vehicle.isActive ? 'on' : 'off'}`}
                  onClick={(e) => {
                    e.stopPropagation();
                    void handleToggleActive(vehicle);
                  }}
                  title={vehicle.isActive ? 'Deactivate vehicle' : 'Activate vehicle'}
                >
                  <span className="toggle-thumb" />
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}