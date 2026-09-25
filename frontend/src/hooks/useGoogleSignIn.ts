import { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { getAuthProviders, googleLogin } from '../api/client';

function loadGsiScript(): Promise<void> {
  return new Promise((resolve, reject) => {
    if (window.google?.accounts) {
      resolve();
      return;
    }
    const existing = document.querySelector<HTMLScriptElement>('script[src="https://accounts.google.com/gsi/client"]');
    if (existing) {
      existing.addEventListener('load', () => resolve(), { once: true });
      existing.addEventListener('error', () => reject(new Error('Google Identity Services script failed to load.')), { once: true });
      return;
    }
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.onload = () => resolve();
    script.onerror = () => reject(new Error('Google Identity Services script failed to load.'));
    document.head.appendChild(script);
  });
}

interface UseGoogleSignInOptions {
  onError?: (message: string) => void;
}

export function useGoogleSignIn({ onError }: UseGoogleSignInOptions = {}) {
  const { setAuth } = useAuth();
  const navigate = useNavigate();
  const [clientId, setClientId] = useState<string | null>(null);
  const [ready, setReady] = useState(false);
  const [configured, setConfigured] = useState(false);
  const buttonRef = useRef<HTMLDivElement>(null);
  const onErrorRef = useRef(onError);

  useEffect(() => {
    onErrorRef.current = onError;
  });

  const handleCredential = useCallback(async (credential: string) => {
    try {
      const res = await googleLogin({ idToken: credential });
      setAuth(res.token, res.user);
      navigate(res.user.role === 'Driver' ? '/driver' : '/');
    } catch {
      onErrorRef.current?.('Google sign-in failed. Please try again.');
    }
  }, [navigate, setAuth]);

  useEffect(() => {
    let cancelled = false;

    (async () => {
      try {
        const providers = await getAuthProviders();
        const id = providers.googleClientId?.trim() || import.meta.env.VITE_GOOGLE_AUTH_CLIENT_ID?.trim() || '';
        if (cancelled) return;

        if (id) {
          await loadGsiScript();
          if (cancelled || !window.google?.accounts) return;

          window.google.accounts.id.initialize({
            client_id: id,
            callback: (response) => {
              void handleCredential(response.credential);
            },
            ux_mode: 'popup',
            auto_select: false,
          });

          setClientId(id);
          setConfigured(true);
        }
        setReady(true);
      } catch {
        setReady(true);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [handleCredential]);

  useEffect(() => {
    if (!clientId || !window.google?.accounts || !buttonRef.current) return;
    buttonRef.current.innerHTML = '';
    window.google.accounts.id.renderButton(buttonRef.current, {
      theme: 'outline',
      size: 'large',
      type: 'standard',
      shape: 'rectangular',
      text: 'continue_with',
      width: 320,
    });
  }, [clientId, ready]);

  return { buttonRef, configured, ready };
}
