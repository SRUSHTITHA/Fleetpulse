import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import { AuthPage } from './pages/AuthPage';
import { ForgotPasswordPage } from './pages/ForgotPasswordPage';
import { ResetPasswordPage } from './pages/ResetPasswordPage';
import { DispatcherDashboard } from './pages/DispatcherDashboard';
import { DriverView } from './pages/DriverView';
import { AdminSettings } from './pages/AdminSettings';
import type { ReactNode } from 'react';

function RouteLoading() {
  return <div className="route-loading">Loading…</div>;
}

function RequireRole({
  allow,
  deniedRedirect,
  children,
}: {
  allow: string[];
  deniedRedirect: (role: string) => string;
  children: ReactNode;
}) {
  const { token, user } = useAuth();
  if (!token) return <Navigate to="/login" replace />;
  if (!user) return <RouteLoading />;
  if (!allow.includes(user.role)) return <Navigate to={deniedRedirect(user.role)} replace />;
  return <>{children}</>;
}

function getHomeRouteForUser(user: { role?: string } | null | undefined): string {
  if (user?.role === 'Driver') return '/driver';
  if (user?.role === 'Admin') return '/admin';
  return '/';
}

function HomeRedirect() {
  const { user, token } = useAuth();
  if (!token) return <Navigate to="/login" replace />;
  if (!user) return <RouteLoading />;
  return <Navigate to={getHomeRouteForUser(user)} replace />;
}

function GuestRoute({ children }: { children: ReactNode }) {
  const { token, user } = useAuth();
  if (token) return <Navigate to={getHomeRouteForUser(user)} replace />;
  return <>{children}</>;
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route
            path="/login"
            element={
              <GuestRoute>
                <AuthPage mode="login" />
              </GuestRoute>
            }
          />
          <Route
            path="/signup"
            element={
              <GuestRoute>
                <AuthPage mode="signup" />
              </GuestRoute>
            }
          />
          <Route
            path="/forgot-password"
            element={
              <GuestRoute>
                <ForgotPasswordPage />
              </GuestRoute>
            }
          />
          <Route
            path="/reset-password"
            element={
              <GuestRoute>
                <ResetPasswordPage />
              </GuestRoute>
            }
          />
          <Route
            path="/"
            element={
              <RequireRole
                allow={['Admin', 'Dispatcher']}
                deniedRedirect={(role) => (role === 'Driver' ? '/driver' : '/login')}
              >
                <DispatcherDashboard />
              </RequireRole>
            }
          />
          <Route
            path="/driver"
            element={
              <RequireRole allow={['Driver']} deniedRedirect={() => '/login'}>
                <DriverView />
              </RequireRole>
            }
          />
          <Route
            path="/admin"
            element={
              <RequireRole allow={['Admin']} deniedRedirect={() => '/'}>
                <AdminSettings />
              </RequireRole>
            }
          />
          <Route path="*" element={<HomeRedirect />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
