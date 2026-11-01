import { useState, type FormEvent } from 'react';
import { Navigate, useLocation } from 'react-router';
import { Button } from '../../components/ui/Button';
import { BrandLogo } from '../../components/layout/BrandLogo';
import { LanguageSwitcher } from '../../components/layout/LanguageSwitcher';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useAdminAuth, safeAdminReturn } from './authContext';
import { adminApi } from './api';
import { ApiError } from '../../services/apiClient';
import { useAdminCopy } from './copy';
import styles from './Admin.module.scss';
export function LoginPage() {
  const t = useAdminCopy();
  usePageMetadata(t.loginTitle);
  const auth = useAdminAuth();
  const location = useLocation();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [show, setShow] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  async function submit(e: FormEvent) {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setError(undefined);
    try {
      auth.setUser(await adminApi.login(email, password));
      setPassword('');
    } catch (error) {
      setError(error instanceof ApiError ? (error.status ?? 0) : 0);
      setPassword('');
    } finally {
      setBusy(false);
    }
  }
  const state: unknown = location.state;
  const returnTo =
    typeof state === 'object' && state !== null && 'returnTo' in state
      ? state.returnTo
      : undefined;
  if (auth.user) return <Navigate to={safeAdminReturn(returnTo)} replace />;
  return (
    <div className={styles.loginPage}>
      <header className={styles.header}>
        <BrandLogo />
        <LanguageSwitcher />
      </header>
      <main id="main-content" className={styles.loginCard}>
        <p className={styles.eyebrow}>{t.admin}</p>
        <h1>{t.loginTitle}</h1>
        <p>{t.loginDescription}</p>
        <form onSubmit={(e) => void submit(e)} aria-busy={busy}>
          <label>
            {t.email}
            <input
              type="email"
              autoComplete="username"
              required
              maxLength={254}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={busy}
            />
          </label>
          <label>
            {t.password}
            <input
              type={show ? 'text' : 'password'}
              autoComplete="current-password"
              required
              maxLength={256}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={busy}
              aria-invalid={error === 401}
              aria-describedby={error === 401 ? 'login-error' : undefined}
            />
          </label>
          <Button
            variant="text"
            aria-pressed={show}
            onClick={() => setShow((v) => !v)}
          >
            {show ? t.hide : t.show}
          </Button>
          {error !== undefined ? (
            <p id="login-error" role="alert">
              {error === 401
                ? t.loginError
                : error === 429
                  ? t.rate
                  : error === 400
                    ? t.csrf
                    : t.error}
            </p>
          ) : null}
          <Button type="submit" disabled={busy}>
            {busy ? t.loading : t.login}
          </Button>
        </form>
      </main>
    </div>
  );
}
