import { useEffect, useRef, useState, type ReactNode } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router';
import { CustomerSessionContext, useCustomerSession } from './context';
import { ApiError } from '../../services/apiClient';
import { useLanguage } from '../../localization/useLanguage';
import { Button } from '../../components/ui/Button';
import { customerAuthApi, customerExpiredEvent } from './api';
import { useCustomerCopy } from './copy';
import type { CustomerUser } from './types';
export function CustomerSessionProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CustomerUser | null>();
  const [failed, setFailed] = useState(false);
  const [attempt, setAttempt] = useState(0);
  const generation = useRef(0),
    initialized = useRef(false);
  const { setLanguage } = useLanguage();
  function update(next: CustomerUser | null) {
    generation.current++;
    setUser((previous) =>
      next && previous?.id === next.id
        ? {
            ...next,
            sessionExpiresAt:
              next.sessionExpiresAt ?? previous.sessionExpiresAt,
          }
        : next,
    );
    setFailed(false);
  }
  useEffect(() => {
    const controller = new AbortController();
    async function check() {
      const started = generation.current;
      try {
        const next = await customerAuthApi.me(controller.signal);
        if (!controller.signal.aborted && started === generation.current) {
          setUser(next);
          setFailed(false);
          if (!initialized.current) {
            initialized.current = true;
            setLanguage(next.preferredLanguage);
          }
        }
      } catch (e) {
        if (controller.signal.aborted || started !== generation.current) return;
        if (e instanceof ApiError && [401, 403].includes(e.status ?? 0)) {
          setUser(null);
          setFailed(false);
        } else setFailed(true);
      }
    }
    void check();
    const timer = setInterval(() => void check(), 60000);
    const focus = () => void check();
    const expire = () => {
      generation.current++;
      setUser(null);
      setFailed(false);
    };
    window.addEventListener('focus', focus);
    window.addEventListener(customerExpiredEvent, expire);
    return () => {
      controller.abort();
      clearInterval(timer);
      window.removeEventListener('focus', focus);
      window.removeEventListener(customerExpiredEvent, expire);
    };
  }, [attempt, setLanguage]);
  return (
    <CustomerSessionContext
      value={{
        user,
        failed,
        setUser: update,
        refresh: () => setAttempt((v) => v + 1),
      }}
    >
      {children}
    </CustomerSessionContext>
  );
}
export function CustomerRoot() {
  return (
    <CustomerSessionProvider>
      <Outlet />
    </CustomerSessionProvider>
  );
}
export function RequireCustomer() {
  const auth = useCustomerSession();
  const t = useCustomerCopy();
  const location = useLocation();
  if (auth.failed)
    return (
      <main id="main-content">
        <p role="alert">{t.error}</p>
        <Button onClick={auth.refresh}>{t.retry}</Button>
      </main>
    );
  if (auth.user === undefined)
    return (
      <main id="main-content">
        <p role="status">{t.loading}</p>
      </main>
    );
  if (!auth.user)
    return (
      <Navigate to="/login" replace state={{ returnTo: location.pathname }} />
    );
  return <Outlet />;
}
