import { useEffect, useState } from 'react';
import { ApiError } from '../services/apiClient';
export function useApiResource<T>(
  key: string,
  load: (signal: AbortSignal) => Promise<T>,
) {
  const [state, setState] = useState<{ key: string; data?: T; error?: number }>(
    { key: '' },
  );
  useEffect(() => {
    const controller = new AbortController();
    void load(controller.signal)
      .then((data) => {
        if (!controller.signal.aborted) setState({ key, data });
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted)
          setState({
            key,
            error: error instanceof ApiError ? (error.status ?? 0) : 0,
          });
      });
    return () => controller.abort();
  }, [key, load]);
  return state.key === key ? state : { key };
}
