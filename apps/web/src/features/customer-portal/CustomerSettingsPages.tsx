import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router';
import { Button } from '../../components/ui/Button';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useLanguage } from '../../localization/useLanguage';
import { ApiError } from '../../services/apiClient';
import { customerAuthApi, customerPortalApi } from './api';
import { useCustomerCopy } from './copy';
import { useCustomerSession } from './context';
import { CustomerError, Field } from './PortalShared';
import { CustomerLogout } from './CustomerLayout';
import type { CustomerUser, Profile } from './types';
import styles from './Customer.module.scss';
function ProfileForm({ user }: { user: CustomerUser }) {
  const t = useCustomerCopy();
  const { setLanguage } = useLanguage();
  const auth = useCustomerSession();
  const [form, setForm] = useState<Profile>({
    firstName: user.firstName,
    lastName: user.lastName,
    phone: user.phone ?? '',
    preferredLanguage: user.preferredLanguage,
  });
  const [busy, setBusy] = useState(false),
    [saved, setSaved] = useState(false),
    [error, setError] = useState<number>();
  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setSaved(false);
    setError(undefined);
    try {
      const u = await customerPortalApi.profile(form);
      auth.setUser(u);
      setLanguage(u.preferredLanguage);
      setSaved(true);
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <form className={styles.form} onSubmit={(e) => void submit(e)}>
      <fieldset disabled={busy}>
        <Field
          label={t.firstName}
          autoComplete="given-name"
          required
          maxLength={100}
          value={form.firstName}
          onChange={(e) => setForm({ ...form, firstName: e.target.value })}
        />
        <Field
          label={t.lastName}
          autoComplete="family-name"
          required
          maxLength={100}
          value={form.lastName}
          onChange={(e) => setForm({ ...form, lastName: e.target.value })}
        />
        <Field
          label={t.phone}
          type="tel"
          autoComplete="tel"
          maxLength={40}
          value={form.phone}
          onChange={(e) => setForm({ ...form, phone: e.target.value })}
        />
        <label>
          {t.language}
          <select
            aria-label={t.language}
            value={form.preferredLanguage}
            onChange={(e) =>
              setForm({
                ...form,
                preferredLanguage: e.target.value === 'en' ? 'en' : 'sv',
              })
            }
          >
            <option value="sv">Svenska</option>
            <option value="en">English</option>
          </select>
        </label>
        <Button type="submit" disabled={busy}>
          {busy ? t.saving : t.save}
        </Button>
      </fieldset>
      {saved && <p role="status">{t.saved}</p>}
      {error !== undefined && <CustomerError status={error} />}
    </form>
  );
}
export function CustomerProfilePage() {
  const t = useCustomerCopy();
  usePageMetadata(t.profile);
  const auth = useCustomerSession();
  return (
    <>
      <h1>{t.profile}</h1>
      <section className={styles.panel}>
        {auth.user && (
          <>
            <p>
              {t.verifiedEmail}: {auth.user.email}
            </p>
            <p>{t.emailReadOnly}</p>
            <ProfileForm key={auth.user.id} user={auth.user} />
          </>
        )}
      </section>
    </>
  );
}
export function CustomerSecurityPage() {
  const t = useCustomerCopy();
  usePageMetadata(t.security);
  const auth = useCustomerSession();
  const navigate = useNavigate();
  const [current, setCurrent] = useState(''),
    [password, setPassword] = useState(''),
    [confirmation, setConfirmation] = useState('');
  const [busy, setBusy] = useState(false),
    [error, setError] = useState<number>();
  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      await customerAuthApi.password(current, password, confirmation);
      auth.setUser(null);
      void navigate('/login', {
        replace: true,
        state: { passwordChanged: true },
      });
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      <h1>{t.security}</h1>
      <section className={styles.panel}>
        <h2>{t.session}</h2>
        <p>
          {auth.user?.sessionExpiresAt
            ? t.expires +
              ': ' +
              new Date(auth.user.sessionExpiresAt).toLocaleString()
            : t.sessionUnknown}
        </p>
        <p>{t.revokeHint}</p>
        <CustomerLogout />
      </section>
      <section className={styles.panel}>
        <h2>{t.passwordChange}</h2>
        <p>{t.passwordHint}</p>
        <form className={styles.form} onSubmit={(e) => void submit(e)}>
          <fieldset disabled={busy}>
            <Field
              label={t.currentPassword}
              type="password"
              autoComplete="current-password"
              required
              maxLength={256}
              value={current}
              onChange={(e) => setCurrent(e.target.value)}
            />
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
            <Button type="submit" disabled={busy}>
              {busy ? t.saving : t.passwordChange}
            </Button>
          </fieldset>
          {error !== undefined && <CustomerError status={error} />}
        </form>
      </section>
    </>
  );
}
