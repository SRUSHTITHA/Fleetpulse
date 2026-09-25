import type { ExceptionAlertDto } from '../../types';
import { RULE_LABELS } from '../../types';
import { formatDateTime } from '../../utils/helpers';

interface AlertDetailProps {
  alert: ExceptionAlertDto;
  onLocate: (tripId: string) => void;
  onDelete: (alert: ExceptionAlertDto) => void;
  onClose: () => void;
}

export function AlertDetail({ alert, onLocate, onDelete, onClose }: AlertDetailProps) {
  return (
    <div className="modal-overlay">
      <div className="modal alert-detail-modal">
        <div className="modal-header">
          <h2>Alert Details</h2>
          <button className="modal-close" onClick={onClose} aria-label="Close">
            x
          </button>
        </div>
        <div className="modal-body">
          <div className="alert-detail-top">
            <span className={`severity-badge ${alert.severity.toLowerCase()}`}>
              {alert.severity}
            </span>
            <span className={`status-chip ${alert.status.toLowerCase()}`}>
              {alert.status}
            </span>
          </div>

          <div className="alert-detail-title">{alert.title}</div>
          <div className="alert-detail-desc">{alert.description}</div>

          <div className="alert-detail-grid">
            <div>
              <dt>Rule</dt>
              <dd>{RULE_LABELS[alert.ruleType]}</dd>
            </div>
            <div>
              <dt>Detected</dt>
              <dd>{formatDateTime(alert.detectedAt)}</dd>
            </div>
            <div>
              <dt>Vehicle</dt>
              <dd>
                {alert.vehicleName} <span className="plate">{alert.licensePlate}</span>
              </dd>
            </div>
            <div>
              <dt>Driver</dt>
              <dd>{alert.driverName || '—'}</dd>
            </div>
            <div>
              <dt>Route</dt>
              <dd>
                {alert.origin} → {alert.destination}
              </dd>
            </div>
            <div>
              <dt>Position</dt>
              <dd className="mono">
                {alert.latitude.toFixed(5)}, {alert.longitude.toFixed(5)}
              </dd>
            </div>
          </div>

          <div className="modal-actions">
            <button
              type="button"
              className="btn-danger"
              onClick={() => onDelete(alert)}
            >
              Delete
            </button>
            <button type="button" className="btn-ghost" onClick={onClose}>
              Close
            </button>
            <button
              type="button"
              className="btn-primary"
              onClick={() => onLocate(alert.tripId)}
            >
              Locate on map
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}