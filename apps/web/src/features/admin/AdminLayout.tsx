import { usePaymentCopy } from '../payments/copy';
import { useState } from 'react';
import { Link, NavLink, Outlet } from 'react-router';
import { LanguageSwitcher } from '../../components/layout/LanguageSwitcher';
import { BrandLogo } from '../../components/layout/BrandLogo';
import { Button } from '../../components/ui/Button';
import { useAdminAuth } from './authContext';
import { useAdminCopy } from './copy';
import { useQuotationCopy } from '../quotations/copy';
import { adminApi } from './api';
import styles from './Admin.module.scss';
export function AdminLayout() {
  const t = useAdminCopy();
  const payments = usePaymentCopy();
  const quotations = useQuotationCopy();
  const auth = useAdminAuth();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(false);
  async function logout() {
    setBusy(true);
    setError(false);
    try {
      await adminApi.logout();
      auth.setUser(null);
    } catch {
      setError(true);
    } finally {
      setBusy(false);
    }
  }
  return (
    <div className={styles.shell}>
      <a className={styles.skip} href="#main-content">
        {t.skip}
      </a>
      <aside className={styles.sidebar}>
        <BrandLogo />
        <p className={styles.eyebrow}>{t.admin}</p>
        <nav aria-label={t.menu}>
          <NavLink to="/admin" end>
            {t.dashboard}
          </NavLink>
          <NavLink to="/admin/requests">{t.requests}</NavLink>
          <NavLink to="/admin/quotations">{quotations.title}</NavLink>
          <NavLink to="/admin/invoices">{payments.title}</NavLink>
          <NavLink to="/admin/proposals">{t.proposals}</NavLink>
        </nav>
        <Link to="/">{t.home}</Link>
        <Button variant="outline" disabled={busy} onClick={() => void logout()}>
          {busy ? t.loading : t.logout}
        </Button>
        {error ? <p role="alert">{t.error}</p> : null}
      </aside>
      <div className={styles.workspace}>
        <header className={styles.header}>
          <span>{auth.user?.displayName}</span>
          <LanguageSwitcher />
        </header>
        <main id="main-content" className={styles.main} tabIndex={-1}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
