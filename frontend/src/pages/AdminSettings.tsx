import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import {
  getExceptionRules,
  createExceptionRule,
  updateExceptionRule,
  deleteExceptionRule,
  getUsers,
  updateUserRole,
} from '../api/client';
import { RuleForm } from '../components/admin/RuleForm';
import type {
  ExceptionRuleDto,
  ExceptionRuleType,
  ExceptionSeverity,
  CreateExceptionRuleRequest,
  UpdateExceptionRuleRequest,
  UserDto,
  UserRole,
} from '../types';
import { RULE_LABELS, RULE_TYPES, RULE_DESC, RULE_UNITS } from '../types';

export function AdminSettings() {
  const { user, logout } = useAuth();
  const [rules, setRules] = useState<ExceptionRuleDto[]>([]);
  const [users, setUsers] = useState<UserDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [usersError, setUsersError] = useState('');
  const [roleSaving, setRoleSaving] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<ExceptionRuleDto | null>(null);

  const reload = async () => {
    try {
      setRules(await getExceptionRules());
    } catch {
      setError('Failed to load exception rules.');
    } finally {
      setLoading(false);
    }
  };

  const reloadUsers = async () => {
    try {
      setUsers(await getUsers());
      setUsersError('');
    } catch {
      setUsersError('Failed to load users.');
    }
  };

  useEffect(() => {
    reload();
    reloadUsers();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSave = async (data: {
    ruleType: ExceptionRuleType;
    thresholdMinutes?: number;
    thresholdPercent?: number;
    thresholdValue?: number;
    severity: ExceptionSeverity;
    isEnabled: boolean;
  }) => {
    if (editing) {
      const req: UpdateExceptionRuleRequest = {
        thresholdMinutes: data.thresholdMinutes,
        thresholdPercent: data.thresholdPercent,
        thresholdValue: data.thresholdValue,
        severity: data.severity,
        isEnabled: data.isEnabled,
      };
      await updateExceptionRule(editing.id, req);
    } else {
      const req: CreateExceptionRuleRequest = {
        ruleType: data.ruleType,
        thresholdMinutes: data.thresholdMinutes,
        thresholdPercent: data.thresholdPercent,
        thresholdValue: data.thresholdValue,
        severity: data.severity,
        isEnabled: data.isEnabled,
      };
      await createExceptionRule(req);
    }
    setShowForm(false);
    setEditing(null);
    await reload();
  };

  const handleToggle = async (rule: ExceptionRuleDto) => {
    try {
      await updateExceptionRule(rule.id, {
        thresholdMinutes: rule.thresholdMinutes,
        thresholdPercent: rule.thresholdPercent,
        thresholdValue: rule.thresholdValue,
        severity: rule.severity,
        isEnabled: !rule.isEnabled,
      });
      await reload();
    } catch {
      setError('Failed to update rule.');
    }
  };

  const handleDelete = async (rule: ExceptionRuleDto) => {
    if (!window.confirm(`Delete the "${RULE_LABELS[rule.ruleType]}" rule?`)) return;
    try {
      await deleteExceptionRule(rule.id);
      await reload();
    } catch {
      setError('Failed to delete rule.');
    }
  };

  const handleRoleChange = async (target: UserDto, role: UserRole) => {
    if (role === target.role) return;
    try {
      setRoleSaving(target.id);
      await updateUserRole(target.id, { role });
      await reloadUsers();
    } catch {
      setUsersError('Failed to update role.');
    } finally {
      setRoleSaving(null);
    }
  };

  const thresholdText = (rule: ExceptionRuleDto) => {
    if (rule.thresholdMinutes != null)
      return `${rule.thresholdMinutes} ${RULE_UNITS[rule.ruleType] ?? 'min'}`;
    if (rule.thresholdPercent != null)
      return `${rule.thresholdPercent} ${RULE_UNITS[rule.ruleType] ?? '%'}`;
    if (rule.thresholdValue != null)
      return `${rule.thresholdValue} ${RULE_UNITS[rule.ruleType] ?? ''}`;
    return '—';
  };

  const currentUserId: string | null = user?.id ?? null;
const allTypesPresent = RULE_TYPES.every((t) =>
  rules.some((r) => r.ruleType === t),
);

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <div className="header-left">
          <div className="logo-mark">FP</div>
          <h1>FleetPulse · Admin</h1>
        </div>
        <div className="header-right">
          <Link to="/" className="btn-nav" role="button">
            Dashboard
          </Link>
          <button className="btn-ghost" onClick={logout}>
            Sign Out
          </button>
        </div>
      </header>

      <div className="admin-main">
        <section className="admin-panel admin-rules-panel">
          <div className="admin-topbar">
            <h2>Exception Rules</h2>
            <button
              className="btn-primary"
              onClick={() => {
                setEditing(null);
                setShowForm(true);
              }}
              disabled={allTypesPresent}
              title={
                allTypesPresent
                  ? 'All four exception types already have a rule — edit an existing one instead.'
                  : 'Add a new exception rule'
              }
            >
              + Add Rule
            </button>
          </div>

          {error && <div className="driver-error">{error}</div>}

          <div className="admin-panel-body">
            {loading ? (
              <div className="admin-card">
                <div className="empty-state">Loading rules…</div>
              </div>
            ) : rules.length === 0 ? (
              <div className="admin-card">
                <div className="empty-state">No exception rules defined yet.</div>
              </div>
            ) : (
              <div className="admin-card">
                <table className="rules-table">
                  <thead>
                    <tr>
                      <th>Rule</th>
                      <th>Description</th>
                      <th>Threshold</th>
                      <th>Severity</th>
                      <th>Status</th>
                      <th className="table-actions">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {rules.map((rule) => (
                      <tr key={rule.id} className={rule.isEnabled ? '' : 'row-disabled'}>
                        <td className="rule-name">{RULE_LABELS[rule.ruleType]}</td>
                        <td className="rule-desc">{RULE_DESC[rule.ruleType]}</td>
                        <td className="mono">{thresholdText(rule)}</td>
                        <td>
                          <span className={`severity-badge ${rule.severity.toLowerCase()}`}>
                            {rule.severity}
                          </span>
                        </td>
                        <td>
                          <button
                            className={`toggle ${rule.isEnabled ? 'on' : 'off'}`}
                            onClick={() => handleToggle(rule)}
                            title={rule.isEnabled ? 'Click to disable' : 'Click to enable'}
                          >
                            <span className="toggle-thumb" />
                          </button>
                        </td>
                        <td className="table-actions">
                          <button
                            className="btn-small"
                            onClick={() => {
                              setEditing(rule);
                              setShowForm(true);
                            }}
                          >
                            Edit
                          </button>
                          <button
                            className="btn-small btn-danger-ghost"
                            onClick={() => handleDelete(rule)}
                          >
                            Delete
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </section>

        <section className="admin-panel admin-users-panel">
          <div className="admin-topbar">
            <h2>Users & Roles</h2>
          </div>

          {usersError && <div className="driver-error">{usersError}</div>}

          <div className="admin-panel-body">
            <div className="admin-card">
              <table className="rules-table">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Email</th>
                    <th>Role</th>
                    <th className="table-actions">Change role</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((u) => (
                    <tr key={u.id} className={u.id === currentUserId ? 'row-disabled' : ''}>
                      <td className="rule-name">{u.fullName || '—'}</td>
                      <td className="rule-desc">{u.email}</td>
                      <td>
                        <span className={`severity-badge ${u.role.toLowerCase()}`}>{u.role}</span>
                      </td>
                      <td className="table-actions">
                        <select
                          className="role-select"
                          value={u.role}
                          disabled={u.id === currentUserId || roleSaving === u.id}
                          onChange={(e) => void handleRoleChange(u, e.target.value as UserRole)}
                          title={
                            u.id === currentUserId
                              ? 'You cannot change your own role'
                              : 'Assign Admin, Dispatcher or Driver role'
                          }
                        >
                          <option value="Admin">Admin</option>
                          <option value="Dispatcher">Dispatcher</option>
                          <option value="Driver">Driver</option>
                        </select>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {users.length === 0 && <div className="empty-state">No users found.</div>}
            </div>
          </div>
        </section>
      </div>

      {showForm && (
        <RuleForm
          initial={editing}
          existingTypes={rules.map((r) => r.ruleType)}
          onSave={handleSave}
          onCancel={() => {
            setShowForm(false);
            setEditing(null);
          }}
        />
      )}
    </div>
  );
}