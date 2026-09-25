import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { login as apiLogin, signUp } from '../api/client';
import { useGoogleSignIn } from '../hooks/useGoogleSignIn';
import { GoogleSignInSection } from '../components/auth/GoogleSignInSection';
import { PasswordField } from '../components/auth/PasswordField';
import {
  getHomeRouteForUser,
  isValidEmail,
  isValidPassword,
  normalizeEmail,
} from '../utils/helpers';

type AuthMode = 'login' | 'signup';

export function AuthPage({ mode }: { mode: AuthMode }) {
  const { setAuth } = useAuth();
  const navigate = useNavigate();
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [licenseNumber, setLicenseNumber] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { buttonRef, configured, ready } = useGoogleSignIn({ onError: setError });

  const isSignUp = mode === 'signup';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    const normalizedEmail = normalizeEmail(email);

    if (isSignUp && !isValidPassword(password)) {
      setError('Password must be at least 8 characters.');
      return;
    }

    if (!isValidEmail(normalizedEmail)) {
      setError('Please enter a valid email address.');
      return;
    }

    setLoading(true);
    try {
      const res = isSignUp
        ? await signUp({
            email: normalizedEmail,
            password,
            fullName,
            licenseNumber: licenseNumber.trim() || undefined,
          })
        : await apiLogin({ email: normalizedEmail, password });
      setAuth(res.token, res.user);
      navigate(getHomeRouteForUser(res.user));
    } catch {
      setError(isSignUp ? 'Sign up failed. That email may already be registered.' : 'Invalid email or password.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-header">
          <div className="login-logo">FP</div>
          <h1>{isSignUp ? 'Create your account' : 'FleetPulse'}</h1>
          <p>{isSignUp ? "You'll be signed up as a Driver" : 'Dispatcher Dashboard'}</p>
        </div>
        {error && <div className="login-error">{error}</div>}
        <form onSubmit={handleSubmit} autoComplete="off">
          {isSignUp && (
            <div className="form-group">
              <label htmlFor="fullName">Full name</label>
              <input
                id="fullName"
                type="text"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                placeholder="Jane Driver"
                required
                autoFocus
                autoComplete="name"
              />
            </div>
          )}
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder= "you@example.com"
              required
              autoFocus={!isSignUp}
              autoComplete="username"
            />
          </div>
          <div className="form-group">
            <label htmlFor="password">Password</label>
            <PasswordField
              value={password}
              onChange={setPassword}
              placeholder={isSignUp ? 'At least 8 characters' : 'Enter password'}
              autoComplete={isSignUp ? 'new-password' : 'current-password'}
            />
          </div>
          {isSignUp && (
            <div className="form-group">
              <label htmlFor="licenseNumber">License number (optional)</label>
              <input
                id="licenseNumber"
                type="text"
                value={licenseNumber}
                onChange={(e) => setLicenseNumber(e.target.value)}
                placeholder="e.g. DL-2024-010"
              />
            </div>
          )}
          <button type="submit" className="btn-primary" disabled={loading}>
            {loading ? (isSignUp ? 'Creating account...' : 'Signing in...') : isSignUp ? 'Create Account' : 'Sign In'}
          </button>
        </form>

        {!isSignUp && (
            <div className="login-footer">
              <Link to="/forgot-password" className="login-link">
                Forgot password?
              </Link>
            </div>
          )}

          <GoogleSignInSection buttonRef={buttonRef} configured={configured} ready={ready} />

        <div className="login-footer">
          <span>{isSignUp ? 'Already have an account?' : "Don't have an account?"}</span>
          <Link to={isSignUp ? '/login' : '/signup'} className="login-link">
            {isSignUp ? 'Sign in' : 'Sign up'}
          </Link>
        </div>
      </div>
    </div>
  );
}