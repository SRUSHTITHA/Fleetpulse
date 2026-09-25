import { useState } from 'react';
import type { DriverDto } from '../../types';
import { VehicleType } from '../../types';
import { geocode } from '../../api/client';

const vehicleTypeLabels: Record<VehicleType, string> = {
  Van: 'Van',
  Truck: 'Truck',
  Motorcycle: 'Motorcycle',
  SmallTruck: 'Small truck',
  LargeContaineredTruck: 'Large containered truck',
};

interface VehicleFormProps {
  drivers: DriverDto[];
  onSave: (data: {
    name: string;
    licensePlate: string;
    type: VehicleType;
    isActive: boolean;
    driverId?: string | null;
    latitude?: number | null;
    longitude?: number | null;
  }) => Promise<void>;
  onCancel: () => void;
}

export function VehicleForm({ drivers, onSave, onCancel }: VehicleFormProps) {
  const [name, setName] = useState('');
  const [licensePlate, setLicensePlate] = useState('');
  const [type, setType] = useState<VehicleType | ''>('');
  const [isActive, setIsActive] = useState(true);
  const [driverId, setDriverId] = useState('');
  const [location, setLocation] = useState('');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!name.trim() || !licensePlate.trim()) {
      setError('Vehicle name and license plate are required.');
      return;
    }
    if (!type) {
      setError('Please select a vehicle type.');
      return;
    }
    if (!driverId) {
      setError('Please assign a driver to the vehicle.');
      return;
    }

    setSaving(true);
    try {
      const coordinates = location.trim() ? await geocode(location) : null;
      if (location.trim() && !coordinates) {
        setError('Could not find that location. Try a city, address, or landmark.');
        return;
      }
      await onSave({
        name: name.trim(),
        licensePlate: licensePlate.trim(),
        type,
        isActive,
        driverId,
        latitude: coordinates?.lat ?? null,
        longitude: coordinates?.lng ?? null,
      });
    } catch {
      setError('Failed to save vehicle.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="modal-overlay">
      <div className="modal">
        <div className="modal-header">
          <h2>Add Vehicle</h2>
          <button className="modal-close" onClick={onCancel} aria-label="Close">
            ×
          </button>
        </div>
        <form onSubmit={handleSubmit} className="modal-body" autoComplete="off">
          {error && <div className="driver-error">{error}</div>}

          <div className="form-group">
            <label htmlFor="vehName">Vehicle name</label>
            <input
              id="vehName"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Delivery Van 2"
              required
              autoFocus
            />
          </div>

          <div className="form-group">
            <label htmlFor="vehPlate">License plate</label>
            <input
              id="vehPlate"
              type="text"
              value={licensePlate}
              onChange={(e) => setLicensePlate(e.target.value)}
              placeholder="e.g. AF-6006"
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="vehType">Type</label>
            <select
              id="vehType"
              value={type}
              onChange={(e) => setType(e.target.value as VehicleType)}
              required
            >
              <option value="" disabled>
                Select type…
              </option>
              {Object.values(VehicleType).map((t) => (
                <option key={t} value={t}>
                  {vehicleTypeLabels[t]}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="vehDriver">Assign to driver</label>
            <select
              id="vehDriver"
              value={driverId}
              onChange={(e) => setDriverId(e.target.value)}
              required
            >
              <option value="" disabled>
                Select a driver…
              </option>
              {drivers.map((d) => (
                <option key={d.id} value={d.id}>
                  {d.fullName} {d.vehicleId ? '(has vehicle)' : ''}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="vehLocation">Current location</label>
            <input
              id="vehLocation"
              type="text"
              value={location}
              onChange={(e) => setLocation(e.target.value)}
              placeholder="e.g. Chicago, IL"
            />
            <span className="form-help">Used as the starting map location until GPS reports in.</span>
          </div>

          <div className="form-group checkbox-group">
            <label className="checkbox">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(e) => setIsActive(e.target.checked)}
              />
              <span>Vehicle active</span>
            </label>
          </div>

          <div className="modal-actions">
            <button type="button" className="btn-ghost" onClick={onCancel}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={saving}>
              {saving ? 'Saving…' : 'Add Vehicle'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}