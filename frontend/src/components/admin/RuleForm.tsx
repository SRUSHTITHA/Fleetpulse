import { useState } from 'react';
import type {
  ExceptionRuleType,
  ExceptionSeverity,
  ExceptionRuleDto,
} from '../../types';
import { RULE_LABELS, RULE_TYPES } from '../../types';

interface RuleFormProps {
  initial?: ExceptionRuleDto | null;
  existingTypes: ExceptionRuleType[];
  onSave: (data: {
    ruleType: ExceptionRuleType;
    thresholdMinutes?: number;
    thresholdPercent?: number;
    thresholdValue?: number;
    severity: ExceptionSeverity;
    isEnabled: boolean;
  }) => Promise<void>;
  onCancel: () => void;
}

const RULE_DESCRIPTIONS: Record<ExceptionRuleType, string> = {
  StopTooLong: 'Flags a vehicle sitting idle past a set number of minutes.',
  DeliveryTrendingLate: 'Flags a delivery that is falling behind schedule by a set percentage.',
  RouteDeviation: 'Flags a vehicle that strays more than a set distance from the planned route.',
  SpeedAnomaly: 'Flags a vehicle exceeding a speed threshold.',
};

export function RuleForm({ initial, existingTypes, onSave, onCancel }: RuleFormProps) {
  const [ruleType, setRuleType] = useState<ExceptionRuleType | ''>(
    initial?.ruleType ?? '',
  );
  const [minutes, setMinutes] = useState(initial?.thresholdMinutes?.toString() ?? '');
  const [percent, setPercent] = useState(initial?.thresholdPercent?.toString() ?? '');
  const [value, setValue] = useState(initial?.thresholdValue?.toString() ?? '');
  const [severity, setSeverity] = useState<ExceptionSeverity>(initial?.severity ?? 'Warning');
  const [isEnabled, setIsEnabled] = useState<boolean>(initial?.isEnabled ?? true);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  const isEdit = Boolean(initial);

  const availableTypes = RULE_TYPES.filter((t) =>
    isEdit ? true : !existingTypes.includes(t),
  );

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!isEdit && !ruleType) {
      setError('Please choose an exception type.');
      return;
    }

    const type =
      ruleType ||
      (initial?.ruleType as ExceptionRuleType);

    const payloadMinutes = minutes !== '' ? Number(minutes) : undefined;
    const payloadPercent = percent !== '' ? Number(percent) : undefined;
    const payloadValue = value !== '' ? Number(value) : undefined;

    if (
      type === 'StopTooLong' &&
      (payloadMinutes === undefined || payloadMinutes <= 0)
    ) {
      setError('Stop Too Long requires a positive threshold in minutes.');
      return;
    }
    if (
      type === 'DeliveryTrendingLate' &&
      (payloadPercent === undefined || payloadPercent <= 0)
    ) {
      setError('Delivery Trending Late requires a positive threshold percent.');
      return;
    }
    if (
      (type === 'RouteDeviation' || type === 'SpeedAnomaly') &&
      (payloadValue === undefined || payloadValue <= 0)
    ) {
      setError('This rule type requires a positive threshold value.');
      return;
    }

    setSaving(true);
    try {
      await onSave({
        ruleType: type,
        thresholdMinutes: payloadMinutes,
        thresholdPercent: payloadPercent,
        thresholdValue: payloadValue,
        severity,
        isEnabled,
      });
    } catch {
      setError('Failed to save rule.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="modal-overlay">
      <div className="modal">
        <div className="modal-header">
          <h2>{isEdit ? 'Edit Exception Rule' : 'Add Exception Rule'}</h2>
          <button className="modal-close" onClick={onCancel} aria-label="Close">
            ×
          </button>
        </div>
        <form onSubmit={handleSubmit} className="modal-body">
          {error && <div className="driver-error">{error}</div>}

          {!isEdit && (
            <div className="form-group">
              <label>Exception type</label>
              <div className="rule-type-grid">
                {availableTypes.map((t) => (
                  <button
                    key={t}
                    type="button"
                    className={`rule-type-card ${ruleType === t ? 'selected' : ''}`}
                    onClick={() => setRuleType(t)}
                    aria-pressed={ruleType === t}
                  >
                    <span className="rule-type-name">{RULE_LABELS[t]}</span>
                    <span className="rule-type-desc">{RULE_DESCRIPTIONS[t]}</span>
                  </button>
                ))}
                {availableTypes.length === 0 && (
                  <div className="empty-state">All exception types already have a rule.</div>
                )}
              </div>
            </div>
          )}

          {isEdit && (
            <div className="form-group">
              <label>Exception type</label>
              <div className="readonly-value">{RULE_LABELS[initial!.ruleType]}</div>
            </div>
          )}

          {(ruleType === 'StopTooLong' || isEdit && initial?.ruleType === 'StopTooLong') && (
            <div className="form-group">
              <label htmlFor="minutes">Threshold (minutes)</label>
              <input
                id="minutes"
                type="number"
                min="1"
                value={minutes}
                onChange={(e) => setMinutes(e.target.value)}
                placeholder="e.g. 10"
              />
            </div>
          )}

          {(ruleType === 'DeliveryTrendingLate' ||
            (isEdit && initial?.ruleType === 'DeliveryTrendingLate')) && (
            <div className="form-group">
              <label htmlFor="percent">Deficit threshold (%)</label>
              <input
                id="percent"
                type="number"
                min="1"
                max="100"
                value={percent}
                onChange={(e) => setPercent(e.target.value)}
                placeholder="e.g. 15"
              />
            </div>
          )}

          {(ruleType === 'RouteDeviation' ||
            ruleType === 'SpeedAnomaly' ||
            (isEdit &&
              (initial?.ruleType === 'RouteDeviation' ||
                initial?.ruleType === 'SpeedAnomaly'))) && (
            <div className="form-group">
              <label htmlFor="value">
                Threshold {ruleType === 'RouteDeviation' || initial?.ruleType === 'RouteDeviation' ? '(meters)' : '(km/h)'}
              </label>
              <input
                id="value"
                type="number"
                min="1"
                value={value}
                onChange={(e) => setValue(e.target.value)}
                placeholder={
                  ruleType === 'RouteDeviation' || initial?.ruleType === 'RouteDeviation'
                    ? 'e.g. 500'
                    : 'e.g. 120'
                }
              />
            </div>
          )}

          <div className="form-group">
            <label htmlFor="severity">Severity</label>
            <select
              id="severity"
              value={severity}
              onChange={(e) => setSeverity(e.target.value as ExceptionSeverity)}
            >
              <option value="Warning">Warning</option>
              <option value="Critical">Critical</option>
            </select>
          </div>

          <div className="form-group checkbox-group">
            <label className="checkbox">
              <input
                type="checkbox"
                checked={isEnabled}
                onChange={(e) => setIsEnabled(e.target.checked)}
              />
              <span>Rule enabled</span>
            </label>
          </div>

          <div className="modal-actions">
            <button type="button" className="btn-ghost" onClick={onCancel}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={saving}>
              {saving ? 'Saving…' : isEdit ? 'Save Changes' : 'Create Rule'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
