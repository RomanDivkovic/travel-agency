import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { Link, useLocation, useNavigate } from 'react-router';
import { BrandLogo } from '../../components/layout/BrandLogo';
import { LanguageSwitcher } from '../../components/layout/LanguageSwitcher';
import { Button } from '../../components/ui/Button';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useLanguage } from '../../localization/useLanguage';
import { ApiError } from '../../services/apiClient';
import { customerAuthApi, customerPortalApi } from './api';
import { useCustomerCopy } from './copy';
import { Field, CustomerError } from './PortalShared';
import { safeCustomerReturn, useCustomerSession } from './context';
import type { Registration } from './types';
import styles from './Customer.module.scss';
export type AuthMode =
  'login' | 'register' | 'forgot-password' | 'resend-verification';
export function AuthShell({ children }: { children: ReactNode }) {
  return (
    <>
      <header className={styles.header}>
        <BrandLogo />
        <LanguageSwitcher />
      </header>
      <main id="main-content" tabIndex={-1} className={styles.auth}>
        {children}
      </main>
    </>
  );
}
export function CustomerAuthForm({
  mode,
  onSignedIn,
}: {
  mode: AuthMode;
  onSignedIn?: () => void;
}) {
  const t = useCustomerCopy();
  const { language, setLanguage } = useLanguage();
  const auth = useCustomerSession(),
    navigate = useNavigate(),
    location = useLocation();
  const [form, setForm] = useState<Registration>({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    passwordConfirmation: '',
    phone: '',
    preferredLanguage: language,
  });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  const [done, setDone] = useState(false);
  const update = <K extends keyof Registration>(
    key: K,
    value: Registration[K],
  ) => setForm((f) => ({ ...f, [key]: value }));
  async function submit(e: FormEvent) {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setError(undefined);
    try {
      if (mode === 'login') {
        const user = await customerAuthApi.login(form.email, form.password);
        auth.setUser(user);
        setLanguage(user.preferredLanguage);
        if (onSignedIn) onSignedIn();
        else {
          const candidate: unknown = location.state?.returnTo;
          void navigate(safeCustomerReturn(candidate), { replace: true });
        }
      } else {
        if (mode === 'register') await customerAuthApi.register(form);
        else await customerAuthApi.emailAction(mode, form.email);
        setDone(true);
        setForm((f) => ({ ...f, password: '', passwordConfirmation: '' }));
      }
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  if (done)
    return (
      <>
        <p role="status">{mode === 'register' ? t.registered : t.checkEmail}</p>
        <Link to="/login">{t.login}</Link>
      </>
    );
  return (
    <form className={styles.form} onSubmit={(e) => void submit(e)}>
      <fieldset disabled={busy}>
        {mode === 'register' && (
          <div className={styles.grid}>
            <Field
              label={t.firstName}
              autoComplete="given-name"
              required
              maxLength={100}
              value={form.firstName}
              onChange={(e) => update('firstName', e.target.value)}
            />
            <Field
              label={t.lastName}
              autoComplete="family-name"
              required
              maxLength={100}
              value={form.lastName}
              onChange={(e) => update('lastName', e.target.value)}
            />
          </div>
        )}
        <Field
          label={t.email}
          type="email"
          autoComplete="username"
          required
          maxLength={254}
          value={form.email}
          onChange={(e) => update('email', e.target.value)}
        />
        {['login', 'register'].includes(mode) && (
          <Field
            label={t.password}
            type="password"
            autoComplete={
              mode === 'login' ? 'current-password' : 'new-password'
            }
            required
            minLength={mode === 'register' ? 12 : 1}
            maxLength={256}
            value={form.password}
            onChange={(e) => update('password', e.target.value)}
          />
        )}{' '}
        {mode === 'register' && (
          <>
            <p>{t.passwordHint}</p>
            <Field
              label={t.confirmPassword}
              type="password"
              autoComplete="new-password"
              required
              minLength={12}
              maxLength={256}
              value={form.passwordConfirmation}
              onChange={(e) => update('passwordConfirmation', e.target.value)}
            />
            <Field
              label={t.phone}
              type="tel"
              autoComplete="tel"
              maxLength={40}
              value={form.phone}
              onChange={(e) => update('phone', e.target.value)}
            />
            <label>
              {t.language}
              <select
                aria-label={t.language}
                value={form.preferredLanguage}
                onChange={(e) =>
                  update(
                    'preferredLanguage',
                    e.target.value === 'en' ? 'en' : 'sv',
                  )
                }
              >
                <option value="sv">Svenska</option>
                <option value="en">English</option>
              </select>
            </label>
          </>
        )}
        <Button type="submit" disabled={busy}>
          {busy
            ? t.saving
            : mode === 'login'
              ? t.login
              : mode === 'register'
                ? t.register
                : mode === 'forgot-password'
                  ? t.resetAction
                  : t.resend}
        </Button>
      </fieldset>
      {error !== undefined && <CustomerError status={error} />}
    </form>
  );
}
export function CustomerAuthPage({ mode }: { mode: AuthMode }) {
  const t = useCustomerCopy();
  const title =
    mode === 'login'
      ? t.login
      : mode === 'register'
        ? t.register
        : mode === 'forgot-password'
          ? t.forgotTitle
          : t.resend;
  usePageMetadata(title);
  const location = useLocation();
  return (
    <AuthShell>
      <h1>{title}</h1>
      {location.state?.passwordChanged && (
        <p role="status">{t.passwordChanged}</p>
      )}
      <p>{t.emailVerifiedOnly}</p>
      <CustomerAuthForm key={mode} mode={mode} />
      <nav className={styles.actions} aria-label={t.portal}>
        <Link to="/login">{t.login}</Link>
        <Link to="/register">{t.register}</Link>
        <Link to="/forgot-password">{t.forgot}</Link>
        <Link to="/resend-verification">{t.resend}</Link>
      </nav>
    </AuthShell>
  );
}
export function CustomerTokenPage({
  action,
}: {
  action: 'verify' | 'reset' | 'claim';
}) {
  const location = useLocation();
  const [token] = useState(() => location.hash.slice(1));
  const t = useCustomerCopy();
  const auth = useCustomerSession();
  const [password, setPassword] = useState(''),
    [confirmation, setConfirmation] = useState('');
  const [busy, setBusy] = useState(false),
    [done, setDone] = useState(false);
  const [error, setError] = useState<number>();
  const title =
    action === 'verify'
      ? t.verifyTitle
      : action === 'reset'
        ? t.resetTitle
        : t.claimConfirmTitle;
  usePageMetadata(title);
  useEffect(() => {
    if (location.hash)
      window.history.replaceState(window.history.state, '', location.pathname);
  }, [location.hash, location.pathname]);
  async function submit(e?: FormEvent) {
    e?.preventDefault();
    if (busy) return;
    setBusy(true);
    setError(undefined);
    try {
      if (action === 'verify') await customerAuthApi.verify(token);
      else if (action === 'reset') {
        await customerAuthApi.reset(token, password, confirmation);
        auth.setUser(null);
      } else await customerPortalApi.confirmClaim(token);
      setDone(true);
      setPassword('');
      setConfirmation('');
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <AuthShell>
      <h1>{title}</h1>
      {done ? (
        <>
          <p role="status">
            {action === 'verify'
              ? t.verified
              : action === 'reset'
                ? t.resetDone
                : t.claimDone}
          </p>
          <Link to={action === 'claim' ? '/account' : '/login'}>
            {action === 'claim' ? t.portal : t.login}
          </Link>
        </>
      ) : !/^[A-Za-z0-9_-]{43}$/.test(token) ? (
        <p role="alert">{t.unavailable}</p>
      ) : action === 'claim' && !auth.user ? (
        <>
          <p>{t.claimLogin}</p>
          {auth.failed ? (
            <CustomerError status={0} retry={auth.refresh} />
          ) : auth.user === undefined ? (
            <p role="status">{t.loading}</p>
          ) : (
            <CustomerAuthForm mode="login" onSignedIn={() => {}} />
          )}
        </>
      ) : (
        <form className={styles.form} onSubmit={(e) => void submit(e)}>
          <fieldset disabled={busy}>
            {action === 'reset' && (
              <>
                <p>{t.passwordHint}</p>
                <Field
                  label={t.newPassword}
                  type="password"
                  autoComplete="new-password"
                  required
                  minLength={12}
                  maxLength={256}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                />
                <Field
                  label={t.confirmPassword}
                  type="password"
                  autoComplete="new-password"
                  required
                  minLength={12}
                  maxLength={256}
                  value={confirmation}
                  onChange={(e) => setConfirmation(e.target.value)}
                />
              </>
            )}
            <Button type="submit" disabled={busy}>
              {busy
                ? t.saving
                : action === 'verify'
                  ? t.verifyAction
                  : action === 'reset'
                    ? t.resetAction
                    : t.claimConfirm}
            </Button>
          </fieldset>
          {error !== undefined && <CustomerError status={error} />}
        </form>
      )}
    </AuthShell>
  );
}
