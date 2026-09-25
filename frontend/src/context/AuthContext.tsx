import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import type { UserDto } from '../types';
import { setAuthToken, getMe } from '../api/client';
import { isApiErrorStatus } from '../utils/helpers';

interface AuthState {
  token: string | null;
  user: UserDto | null;
  setAuth: (token: string, user: UserDto) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

function readSavedUser(): UserDto | null {
  const saved = localStorage.getItem('fp_user');
  if (!saved) return null;

  try {
    return JSON.parse(saved) as UserDto;
  } catch {
    localStorage.removeItem('fp_user');
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => {
    const saved = localStorage.getItem('fp_token');
    if (saved) setAuthToken(saved);
    return saved;
  });
  const [user, setUser] = useState<UserDto | null>(readSavedUser);

  const clearAuthSession = () => {
    setAuthToken(null);
    setToken(null);
    setUser(null);
    localStorage.removeItem('fp_token');
    localStorage.removeItem('fp_user');
  };

  useEffect(() => {
    if (!token) {
      setUser(null);
      return;
    }

    let cancelled = false;
    getMe(token)
      .then((u) => {
        if (cancelled) return;
        setUser(u);
        localStorage.setItem('fp_user', JSON.stringify(u));
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        if (isApiErrorStatus(error, 401) || isApiErrorStatus(error, 403)) {
          clearAuthSession();
        }
      });

    return () => {
      cancelled = true;
    };
  }, [token]);

  const setAuth = (t: string, u: UserDto) => {
    setAuthToken(t);
    setToken(t);
    setUser(u);
    localStorage.setItem('fp_token', t);
    localStorage.setItem('fp_user', JSON.stringify(u));
  };

  const logout = () => {
    clearAuthSession();
  };

  return (
    <AuthContext.Provider value={{ token, user, setAuth, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
