import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useLocation } from 'react-router';
import { BrandLogo } from '../../components/layout/BrandLogo';
import { LanguageSwitcher } from '../../components/layout/LanguageSwitcher';
import { Button } from '../../components/ui/Button';
import { useLanguage } from '../../localization/useLanguage';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useApiResource } from '../../hooks/useApiResource';
import { ApiError } from '../../services/apiClient';
import { publicQuotationApi } from './api';
import { useQuotationCopy } from './copy';
import { QuotationSummary } from './QuotationSummary';
import { QuotationError } from './QuotationError';
import type { CustomerQuotation } from './types';
import styles from './Quotations.module.scss';
function CustomerDecision({
  quotation: q,
  token,
  onConfirmed,
  onRefresh,
}: {
  quotation: CustomerQuotation;
  token: string;
  onConfirmed: (q: CustomerQuotation) => void;
  onRefresh: () => void;
}) {
  const t = useQuotationCopy();
  const [ack, setAck] = useState(false);
  const [decline, setDecline] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  const pending = useRef(false);
  async function respond(accept: boolean) {
    if (pending.current || (accept && !ack)) return;
    pending.current = true;
    setBusy(true);
    setError(undefined);
    try {
      onConfirmed(await publicQuotationApi.respond(token, accept));
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      pending.current = false;
      setBusy(false);
    }
  }
  return (
    <section className={styles.panel}>
      <h2>{t.acceptance}</h2>
      <p>{t.explicit}</p>
      <p>{t.noBooking}</p>
      <label className={styles.checkbox}>
        <input
          type="checkbox"
          checked={ack}
          onChange={(e) => setAck(e.target.checked)}
          disabled={busy}
        />
        <span>{q.acceptanceText}</span>
      </label>
      <div className={styles.actions}>
        <Button
          disabled={!ack || !q.acceptanceText || busy}
          onClick={() => void respond(true)}
        >
          {busy ? t.saving : t.accept}
        </Button>
        <Button
          variant="outline"
          disabled={busy}
          onClick={() => setDecline(true)}
        >
          {t.decline}
        </Button>
      </div>
      {decline ? (
        <section className={styles.confirm}>
          <h3>{t.confirmDecline}</h3>
          <Button disabled={busy} onClick={() => void respond(false)}>
            {t.declineYes}
          </Button>
          <Button
            variant="text"
            disabled={busy}
            onClick={() => setDecline(false)}
          >
            {t.keep}
          </Button>
        </section>
      ) : null}
      {error !== undefined ? (
        <QuotationError status={error} customer retry={onRefresh} />
      ) : null}
    </section>
  );
}
function CustomerView({ token }: { token: string }) {
  const t = useQuotationCopy();
  const { setLanguage } = useLanguage();
  usePageMetadata(t.customerTitle);
  const [revision, setRevision] = useState(0);
  const [confirmed, setConfirmed] = useState<CustomerQuotation>();
  const initialized = useRef(false);
  const heading = useRef<HTMLHeadingElement>(null);
  const load = useCallback(
    (signal: AbortSignal) =>
      /^[A-Za-z0-9_-]{43}$/.test(token)
        ? publicQuotationApi.review(token, signal)
        : Promise.reject(new ApiError('Quotation unavailable', 404)),
    [token],
  );
  const state = useApiResource(`${token}-${revision}`, load);
  const quote = confirmed ?? state.data;
  useEffect(() => {
    if (quote && !initialized.current) {
      initialized.current = true;
      setLanguage(quote.language);
    }
  }, [quote, setLanguage]);
  useEffect(() => {
    if (confirmed) {
      heading.current?.focus({ preventScroll: true });
      heading.current?.scrollIntoView({ block: 'start', behavior: 'instant' });
    }
  }, [confirmed]);
  function refresh() {
    setConfirmed(undefined);
    setRevision((v) => v + 1);
  }
  return (
    <>
      <header className={styles.customerHeader}>
        <BrandLogo />
        <LanguageSwitcher />
      </header>
      <main id="main-content" tabIndex={-1} className={styles.customer}>
        <h1 ref={heading} tabIndex={-1}>
          {quote?.status === 'Accepted'
            ? t.acceptedTitle
            : quote?.status === 'Declined'
              ? t.declinedTitle
              : t.customerTitle}
        </h1>
        <p>{t.privateLink}</p>
        {state.error !== undefined && !confirmed ? (
          <QuotationError status={state.error} customer retry={refresh} />
        ) : !quote ? (
          <p role="status">{t.loading}</p>
        ) : (
          <>
            {quote.status === 'Accepted' ? (
              <p role="status">{t.acceptedMessage}</p>
            ) : quote.status === 'Declined' ? (
              <p role="status">{t.declinedMessage}</p>
            ) : null}
            <QuotationSummary quotation={quote} />
            {quote.status === 'Sent' ? (
              <CustomerDecision
                key={`${quote.number}-${quote.version}`}
                quotation={quote}
                token={token}
                onConfirmed={setConfirmed}
                onRefresh={refresh}
              />
            ) : null}
          </>
        )}
        <Link to="/">{t.home}</Link>
      </main>
    </>
  );
}
export function CustomerQuotationPage() {
  const location = useLocation();
  const token = location.hash.slice(1);
  return <CustomerView key={token} token={token} />;
}
