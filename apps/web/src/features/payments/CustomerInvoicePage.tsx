import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useLocation } from 'react-router';
import { BrandLogo } from '../../components/layout/BrandLogo';
import { LanguageSwitcher } from '../../components/layout/LanguageSwitcher';
import { Button } from '../../components/ui/Button';
import { useApiResource } from '../../hooks/useApiResource';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useLanguage } from '../../localization/useLanguage';
import { ApiError } from '../../services/apiClient';
import { downloadInvoice, publicInvoiceApi, safeCheckoutUrl } from './api';
import { usePaymentCopy } from './copy';
import { InvoiceSummary } from './InvoiceSummary';
import { PaymentError } from './PaymentError';
import styles from './Payments.module.scss';
export function PaymentShell({ children }: { children: React.ReactNode }) {
  return (
    <>
      <header className={styles.header}>
        <BrandLogo />
        <LanguageSwitcher />
      </header>
      <main id="main-content" tabIndex={-1} className={styles.customer}>
        {children}
      </main>
    </>
  );
}
function InvoiceView({ token, result }: { token: string; result: boolean }) {
  const t = usePaymentCopy();
  const { setLanguage } = useLanguage();
  usePageMetadata(result ? t.result : t.customerTitle);
  const initialized = useRef(false);
  const pending = useRef(false);
  const [revision, setRevision] = useState(0);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  const load = useCallback(
    (signal: AbortSignal) =>
      /^[A-Za-z0-9_-]{43}$/.test(token)
        ? publicInvoiceApi.review(token, signal)
        : Promise.reject(new ApiError('Unavailable', 404)),
    [token],
  );
  const state = useApiResource(`${token}-${revision}`, load);
  const i = state.data;
  useEffect(() => {
    if (i && !initialized.current) {
      initialized.current = true;
      setLanguage(i.language);
    }
  }, [i, setLanguage]);
  const verified = useRef(false);
  useEffect(() => {
    if (i?.status === 'Paid') verified.current = true;
  }, [i]);
  // Bounded polling reads authoritative state; it never manufactures a successful outcome.
  useEffect(() => {
    if (!result) return;
    let count = 0;
    const timer = window.setInterval(() => {
      if (verified.current) {
        window.clearInterval(timer);
        return;
      }
      count++;
      setRevision((v) => v + 1);
      if (count >= 12) window.clearInterval(timer);
    }, 5000);
    return () => window.clearInterval(timer);
  }, [result, token]);
  async function pay() {
    if (pending.current || !i) return;
    pending.current = true;
    setBusy(true);
    setError(undefined);
    try {
      const session = await publicInvoiceApi.session(token);
      window.location.assign(
        safeCheckoutUrl(session.checkoutUrl, i.isSimulated),
      );
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
      pending.current = false;
      setBusy(false);
    }
  }
  async function download() {
    setError(undefined);
    try {
      await downloadInvoice(token);
    } catch {
      setError(0);
    }
  }
  const failed =
    i?.lastPaymentStatus === 'Failed' || i?.lastPaymentStatus === 'Cancelled';
  const heading =
    i?.status === 'Paid'
      ? i.isSimulated
        ? t.simulatedDone
        : t.paidTitle
      : result
        ? failed
          ? t.failedTitle
          : t.pendingTitle
        : t.customerTitle;
  return (
    <PaymentShell>
      <h1>{heading}</h1>
      <p>{t.privateLink}</p>
      {state.error !== undefined ? (
        <PaymentError
          status={state.error}
          retry={() => setRevision((v) => v + 1)}
        />
      ) : !i ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          {i.status === 'Paid' ? (
            <p role="status">
              {i.isSimulated ? t.sandboxEligible : t.paidMessage}
            </p>
          ) : result ? (
            <p role="status">{failed ? t.failedMessage : t.pendingMessage}</p>
          ) : null}
          <p>{t.explanation}</p>
          <InvoiceSummary invoice={i} />
          {error !== undefined && (
            <PaymentError
              status={error}
              retry={() => setRevision((v) => v + 1)}
            />
          )}
          <div className={styles.actions}>
            {(['Issued', 'PaymentPending', 'Overdue'] as string[]).includes(
              i.status,
            ) && (
              <Button disabled={busy} onClick={() => void pay()}>
                {busy ? t.busy : i.isSimulated ? t.sandboxPay : t.pay}
              </Button>
            )}
            <Button variant="outline" onClick={() => void download()}>
              {t.download}
            </Button>
            <Button variant="text" onClick={() => setRevision((v) => v + 1)}>
              {t.retry}
            </Button>
          </div>
          <p>{t.documentNote}</p>
          {result && <Link to={'/invoice#' + token}>{t.backInvoice}</Link>}
        </>
      )}
      <p>
        <Link to="/">{t.home}</Link>
      </p>
    </PaymentShell>
  );
}
export function CustomerInvoicePage({ result = false }: { result?: boolean }) {
  const { hash } = useLocation();
  return <InvoiceView key={hash} token={hash.slice(1)} result={result} />;
}
