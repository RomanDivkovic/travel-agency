import { useCallback, useEffect, useState } from 'react';
import { ApiError } from '../../services/apiClient';
export function useRefreshOnReturn(refresh: () => void) {
  useEffect(() => {
    let last = 0;
    const run = () => {
      if (document.visibilityState === 'visible' && Date.now() - last > 500) {
        last = Date.now();
        refresh();
      }
    };
    window.addEventListener('focus', run);
    document.addEventListener('visibilitychange', run);
    return () => {
      window.removeEventListener('focus', run);
      document.removeEventListener('visibilitychange', run);
    };
  }, [refresh]);
}
export function useMessagingResource<T>(
  key: string,
  load: (s: AbortSignal) => Promise<T>,
) {
  const [revision, setRevision] = useState(0);
  const [state, setState] = useState<{ key: string; data?: T; error?: number }>(
    { key: '' },
  );
  const refresh = useCallback(() => setRevision((v) => v + 1), []);
  useRefreshOnReturn(refresh);
  useEffect(() => {
    const controller = new AbortController();
    void load(controller.signal)
      .then((data) => {
        if (!controller.signal.aborted) setState({ key, data });
      })
      .catch((e: unknown) => {
        if (!controller.signal.aborted)
          setState((previous) => ({
            key,
            data: previous.key === key ? previous.data : undefined,
            error: e instanceof ApiError ? (e.status ?? 0) : 0,
          }));
      });
    return () => controller.abort();
  }, [key, load, revision]);
  return { ...(state.key === key ? state : { key }), refresh };
}
