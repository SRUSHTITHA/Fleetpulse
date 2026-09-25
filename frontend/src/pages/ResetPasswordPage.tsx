import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import axios from 'axios';
import { resetPassword } from '../api/client';
import { PasswordField } from '../components/auth/PasswordField';

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const email = searchParams.get('email') ?? '';
  const token = searchParams.get('token') ?? '';
  const hasParams = Boolean(email && token);

  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setMessage('');

    if (password.length < 8) {
      setError('Password must be at least 8 characters.');
      return;
    }
    if (password !== confirm) {
      setError('Passwords do not match.');
      return;
    }

    setLoading(true);
    try {
      const res = await resetPassword({ email, token, newPassword: password });
      setMessage(res.message || 'Your password has been reset. You can sign in now.');
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.message) {
        setError(err.response.data.message);
      } else {
        setError('Something went wrong. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  if (!hasParams) {
    return (
      <div className="login-page">
        <div className="login-card">
          <div className="login-header">
            <div className="login-logo">FP</div>
            <h1>Invalid reset link</h1>
            <p>This link is missing information. Request a fresh one.</p>
          </div>
          <div className="login-error">This reset link is invalid or has expired.</div>
          <div className="login-footer">
            <span>Need a new link?</span>
            <Link to="/forgot-password" className="login-link">
              Request a reset
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-header">
          <div className="login-logo">FP</div>
          <h1>Set a new password</h1>
          <p>{email}</p>
        </div>
        {error && <div className="login-error">{error}</div>}
        {message && <div className="login-message">{message}</div>}
        {!message ? (
          <form onSubmit={handleSubmit} autoComplete="off">
            <div className="form-group">
              <label htmlFor="password">New password</label>
              <PasswordField
                value={password}
                onChange={setPassword}
                placeholder="At least 8 characters"
                autoComplete="new-password"
              />
            </div>
            <div className="form-group">
              <label htmlFor="confirmPassword">Confirm password</label>
              <PasswordField
                value={confirm}
                onChange={setConfirm}
                placeholder="Repeat your password"
                autoComplete="new-password"
              />
            </div>
            <button type="submit" className="btn-primary" disabled={loading}>
              {loading ? 'Resetting...' : 'Reset password'}
            </button>
          </form>
        ) : (
          <div className="login-footer">
            <span>You can now sign in with your new password.</span>
            <Link to="/login" className="login-link">
              Sign in
            </Link>
          </div>
        )}
      </div>
    </div>
  );
}