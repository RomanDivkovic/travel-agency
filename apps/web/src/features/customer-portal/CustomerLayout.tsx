import { useEffect, useRef, useState } from 'react';
import { Link, NavLink, Outlet, useLocation } from 'react-router';
import { BrandLogo } from '../../components/layout/BrandLogo';
import { Button } from '../../components/ui/Button';
import { useLanguage } from '../../localization/useLanguage';
import { ApiError } from '../../services/apiClient';
import { customerAuthApi, customerPortalApi } from './api';
import { useCustomerCopy } from './copy';
import { CustomerError } from './PortalShared';
import { useCustomerSession } from './context';
import styles from './Customer.module.scss';
function CustomerNavigation() {
  const t = useCustomerCopy();
  return (
    <nav className={styles.nav} aria-label={t.navigation}>
      {[
        ['/account', t.overview],
        ['/account/requests', t.requests],
        ['/account/quotations', t.quotations],
        ['/account/invoices', t.invoices],
        ['/account/proposals', t.proposals],
        ['/account/bookings', t.bookings],
        ['/account/profile', t.profile],
        ['/account/security', t.security],
      ].map(([path, label]) => (
        <NavLink key={path} to={path!} end={path === '/account'}>
          {label}
        </NavLink>
      ))}
    </nav>
  );
}
export function CustomerLogout() {
  const t = useCustomerCopy();
  const auth = useCustomerSession();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  return (
    <div>
      <Button
        variant="outline"
        disabled={busy}
        onClick={() => {
          setBusy(true);
          setError(undefined);
          void customerAuthApi
            .logout()
            .then(() => auth.setUser(null))
            .catch((e: unknown) =>
              setError(e instanceof ApiError ? (e.status ?? 0) : 0),
            )
            .finally(() => setBusy(false));
        }}
      >
        {busy ? t.saving : t.logout}
      </Button>
      {error !== undefined && <CustomerError status={error} />}
    </div>
  );
}
function AccountLanguage() {
  const t = useCustomerCopy();
  const { language, setLanguage } = useLanguage();
  const auth = useCustomerSession();
  const [busy, setBusy] = useState(false),
    [error, setError] = useState<number>();
  return (
    <div>
      <label>
        {t.headerLanguage}
        <select
          aria-label={t.headerLanguage}
          value={language}
          disabled={busy}
          onChange={(e) => {
            const next = e.target.value === 'en' ? 'en' : 'sv';
            setBusy(true);
            setError(undefined);
            void customerPortalApi
              .language(next)
              .then((user) => {
                auth.setUser(user);
                setLanguage(user.preferredLanguage);
              })
              .catch((e: unknown) =>
                setError(e instanceof ApiError ? (e.status ?? 0) : 0),
              )
              .finally(() => setBusy(false));
          }}
        >
          <option value="sv">Svenska</option>
          <option value="en">English</option>
        </select>
      </label>
      {error !== undefined && <CustomerError status={error} />}
    </div>
  );
}
export function CustomerLayout() {
  const t = useCustomerCopy();
  const { pathname } = useLocation();
  const main = useRef<HTMLElement>(null);
  useEffect(() => {
    main.current?.focus({ preventScroll: true });
  }, [pathname]);
  return (
    <>
      <a className={styles.skip} href="#main-content">
        {t.skip}
      </a>
      <header className={styles.header}>
        <BrandLogo />
        <div className={styles.headerActions}>
          <AccountLanguage />
          <CustomerLogout />
        </div>
      </header>
      <div className={styles.shell}>
        <aside className={styles.sidebar}>
          <p>{t.portal}</p>
          <CustomerNavigation />
          <Link to="/">{t.home}</Link>
        </aside>
        <details className={styles.mobile} key={pathname}>
          <summary>{t.navigation}</summary>
          <CustomerNavigation />
        </details>
        <main
          ref={main}
          id="main-content"
          tabIndex={-1}
          className={styles.main}
        >
          <Outlet />
        </main>
      </div>
    </>
  );
}
