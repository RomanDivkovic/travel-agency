import { useEffect, useState } from 'react';
import { getApiHealth } from '../services/health';

type ApiStatus = 'checking' | 'connected' | 'unavailable';

export function useApiHealth(): ApiStatus {
  const [status, setStatus] = useState<ApiStatus>('checking');

  useEffect(() => {
    const controller = new AbortController();
    void getApiHealth(controller.signal)
      .then(() => {
        if (!controller.signal.aborted) setStatus('connected');
      })
      .catch(() => {
        if (!controller.signal.aborted) setStatus('unavailable');
      });
    return () => controller.abort();
  }, []);

  return status;
}
