import { useEffect, useRef, useState, type ReactNode } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router';
import { adminApi, expiredEvent } from './api';
import type { AdminUser } from './model';
import { AuthContext, useAdminAuth } from './authContext';
import { ApiError } from '../../services/apiClient';
import { useAdminCopy } from './copy';
import { Button } from '../../components/ui/Button';
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AdminUser | null>();
  const [failed, setFailed] = useState(false);
  const [forbidden, setForbidden] = useState(false);
  const [attempt, setAttempt] = useState(0);
  const generation = useRef(0);
  function updateUser(value: AdminUser | null) {
    generation.current++;
    setUser(value);
    setFailed(false);
    setForbidden(false);
  }
  useEffect(() => {
    const controller = new AbortController();
    async function check() {
      const started = generation.current;
      try {
        const user = await adminApi.me(controller.signal);
        if (!controller.signal.aborted && started === generation.current) {
          setUser(user);
          setFailed(false);
          setForbidden(false);
        }
      } catch (error) {
        if (controller.signal.aborted || started !== generation.current) return;
        if (error instanceof ApiError && error.status === 401) {
          setUser(null);
          setFailed(false);
        } else if (error instanceof ApiError && error.status === 403) {
          setUser(null);
          setForbidden(true);
        } else setFailed(true);
      }
    }
    void check();
    const id = window.setInterval(() => void check(), 60000);
    const focus = () => void check();
    window.addEventListener('focus', focus);
    const expire = () => {
      generation.current++;
      setUser(null);
      setFailed(false);
    };
    window.addEventListener(expiredEvent, expire);
    return () => {
      controller.abort();
      clearInterval(id);
      window.removeEventListener('focus', focus);
      window.removeEventListener(expiredEvent, expire);
    };
  }, [attempt]);
  return (
    <AuthContext
      value={{
        user,
        failed,
        forbidden,
        setUser: updateUser,
        refresh: () => setAttempt((v) => v + 1),
      }}
    >
      {children}
    </AuthContext>
  );
}
export function RequireAdmin() {
  const auth = useAdminAuth();
  const t = useAdminCopy();
  const location = useLocation();
  if (auth.forbidden)
    return (
      <main id="main-content">
        <h1>{t.forbidden}</h1>
      </main>
    );
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
      <Navigate
        to="/admin/login"
        replace
        state={{ returnTo: location.pathname }}
      />
    );
  return <Outlet />;
}
