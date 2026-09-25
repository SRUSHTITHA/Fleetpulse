import { useCallback, useEffect, useRef, useState } from 'react';

export interface GeolocationState {
  lat: number;
  lng: number;
  speedKmh?: number;
  error?: string;
}

export type GeoPermission = 'prompt' | 'granted' | 'denied' | 'unsupported' | 'unknown';

interface Options {
  enabled: boolean;
  intervalMs?: number;
}

const DEFAULT_INTERVAL = 30000;

export function useGeolocation({ enabled, intervalMs = DEFAULT_INTERVAL }: Options) {
  const [state, setState] = useState<GeolocationState | null>(null);
  const [error, setError] = useState<string | undefined>();
  const [permission, setPermission] = useState<GeoPermission>('unknown');
  const watchIdRef = useRef<number | null>(null);
  const timerRef = useRef<number | null>(null);

  const stop = useCallback(() => {
    if (watchIdRef.current != null) {
      navigator.geolocation.clearWatch(watchIdRef.current);
      watchIdRef.current = null;
    }
    if (timerRef.current != null) {
      window.clearTimeout(timerRef.current);
      timerRef.current = null;
    }
  }, []);

  const onSuccess = useCallback((pos: GeolocationPosition) => {
    const coords = pos.coords;
    setState({
      lat: coords.latitude,
      lng: coords.longitude,
      speedKmh: coords.speed != null && coords.speed >= 0 ? coords.speed * 3.6 : undefined,
    });
    setError(undefined);
    setPermission('granted');
  }, []);

  const onError = useCallback((err: GeolocationPositionError) => {
    if (err.code === err.PERMISSION_DENIED) {
      setPermission('denied');
      setError('Location sharing is mandatory to use FleetPulse. Allow it for this site, then tap Try again.');
    } else {
      setError(err.message || 'Unable to access location.');
    }
  }, []);

  const startWatch = useCallback(() => {
    if (!navigator.geolocation) return;
    stop();

    watchIdRef.current = navigator.geolocation.watchPosition(onSuccess, onError, {
      enableHighAccuracy: true,
      maximumAge: 3000,
      timeout: 15000,
    });

    const poll = () => {
      navigator.geolocation.getCurrentPosition(onSuccess, onError, {
        enableHighAccuracy: true,
        maximumAge: 5000,
        timeout: 10000,
      });
      timerRef.current = window.setTimeout(poll, intervalMs);
    };
    timerRef.current = window.setTimeout(poll, intervalMs);
  }, [intervalMs, onError, onSuccess, stop]);

  const requestLocation = useCallback(() => {
    if (!navigator.geolocation) {
      setPermission('unsupported');
      setError('Geolocation is not supported by this browser.');
      return;
    }

    // A user gesture can re-open the browser prompt when the state is still
    // "prompt". If it is already "denied", this call fails immediately until
    // they change the site setting, then it succeeds on the next try.
    navigator.geolocation.getCurrentPosition(onSuccess, onError, {
      enableHighAccuracy: true,
      maximumAge: 0,
      timeout: 20000,
    });
    startWatch();
  }, [onError, onSuccess, startWatch]);

  useEffect(() => {
    if (!enabled) {
      setState(null);
      stop();
      return;
    }

    if (!navigator.geolocation) {
      setPermission('unsupported');
      setError('Location sharing is not supported by this browser.');
      return;
    }

    let permissionStatus: PermissionStatus | undefined;

    const handleState = (state: PermissionState) => {
      setPermission(state);
      if (state === 'granted') {
        startWatch();
      } else if (state === 'prompt') {
        // Ask right away: this coordinates call is what pops the browser prompt.
        requestLocation();
      } else if (state === 'denied') {
        stop();
        setError('Location sharing is mandatory to use FleetPulse. Allow it for this site, then tap Try again.');
      }
    };

    if (navigator.permissions?.query) {
      navigator.permissions
        .query({ name: 'geolocation' })
        .then((status) => {
          permissionStatus = status;
          handleState(status.state);
          status.onchange = () => handleState(status.state);
        })
        .catch(() => {
          requestLocation();
        });
    } else {
      requestLocation();
    }

    const onVisible = () => {
      if (document.visibilityState === 'visible' && navigator.permissions?.query) {
        navigator.permissions.query({ name: 'geolocation' }).then((status) => {
          handleState(status.state);
        }).catch(() => {});
      }
    };
    document.addEventListener('visibilitychange', onVisible);

    return () => {
      document.removeEventListener('visibilitychange', onVisible);
      if (permissionStatus) permissionStatus.onchange = null;
      stop();
    };
  }, [enabled, requestLocation, startWatch, stop]);

  return { state, error, permission, requestLocation, stop };
}
