import type { ExceptionAlertDto } from '../../types';
import { formatDateTime } from '../../utils/helpers';

interface AlertPanelProps {
  alerts: ExceptionAlertDto[];
  onOpen: (alert: ExceptionAlertDto) => void;
  onDelete: (alert: ExceptionAlertDto) => void;
}

export function AlertPanel({ alerts, onOpen, onDelete }: AlertPanelProps) {
  return (
    <div className="alert-panel">
      <div className="panel-header">
        <h2>Alerts</h2>
        {alerts.length > 0 && (
          <span className="count-badge alert-count">{alerts.length}</span>
        )}
      </div>
      <div className="alert-items">
        {alerts.length === 0 && (
          <div className="empty-state">No alerts yet</div>
        )}
        {alerts.map((alert) => (
          <div
            key={alert.id}
            className={`alert-item severity-${alert.severity.toLowerCase()}`}
            onClick={() => onOpen(alert)}
          >
            <div className="alert-top">
              <span className={`severity-badge ${alert.severity.toLowerCase()}`}>
                {alert.severity}
              </span>
              <span className="alert-time">{formatDateTime(alert.detectedAt)}</span>
            </div>
            <div className="alert-title">{alert.title}</div>
            <div className="alert-desc">{alert.description}</div>
            <div className="alert-meta">
              <span>{alert.vehicleName}</span>
              <span className="divider">·</span>
              <span>{alert.driverName}</span>
            </div>
            <button
              className="alert-delete-btn"
              onClick={(e) => {
                e.stopPropagation();
                onDelete(alert);
              }}
              title="Permanently delete this alert"
              aria-label={`Delete alert ${alert.title}`}
            >
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M3 6h18" />
                <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6" />
                <path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
              </svg>
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}