import { useMoney } from './useMoney';
import { useCallback, useState, type FormEvent } from 'react';
import { Link, useParams } from 'react-router';
import { Button } from '../../components/ui/Button';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useApiResource } from '../../hooks/useApiResource';
import { ApiError } from '../../services/apiClient';
import { downloadInvoice, invoiceApi } from './api';
import { usePaymentCopy } from './copy';
import { InvoiceSummary } from './InvoiceSummary';
import { PaymentError } from './PaymentError';
import type { AdminInvoice } from './types';
import styles from './Payments.module.scss';
function DraftEditor({
  detail: d,
  onSaved,
}: {
  detail: AdminInvoice;
  onSaved: () => void;
}) {
  const t = usePaymentCopy();
  const [address, setAddress] = useState(d.invoice.customerAddress);
  const [period, setPeriod] = useState(d.invoice.servicePeriod);
  const [due, setDue] = useState(d.invoice.dueAt.slice(0, 10));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  async function save(e: FormEvent) {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setError(undefined);
    try {
      await invoiceApi.save(d.id, {
        versionToken: d.versionToken,
        customerAddress: address,
        servicePeriod: period,
        dueAt: due + 'T21:59:59Z',
      });
      onSaved();
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className={styles.panel}>
      <h2>{t.draft}</h2>
      <p>{t.immutable}</p>
      <form className={styles.form} onSubmit={(e) => void save(e)}>
        <label>
          {t.address}
          <textarea
            aria-label={t.address}
            required
            maxLength={1000}
            value={address}
            disabled={busy}
            onChange={(e) => setAddress(e.target.value)}
          />
        </label>
        <label>
          {t.period}
          <input
            required
            maxLength={300}
            value={period}
            disabled={busy}
            onChange={(e) => setPeriod(e.target.value)}
          />
        </label>
        <label>
          {t.due}
          <input
            type="date"
            required
            value={due}
            disabled={busy}
            onChange={(e) => setDue(e.target.value)}
          />
        </label>
        <Button type="submit" disabled={busy}>
          {busy ? t.busy : t.save}
        </Button>
      </form>
      {error !== undefined && <PaymentError status={error} />}
    </section>
  );
}
export function AdminInvoiceDetailPage() {
  const { id = '' } = useParams();
  const t = usePaymentCopy();
  const money = useMoney();
  usePageMetadata(t.detail);
  const [revision, setRevision] = useState(0);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  const [message, setMessage] = useState('');
  const [confirmation, setConfirmation] = useState<
    'issue' | 'cancel' | 'revoke-access'
  >();
  const load = useCallback(
    async (signal: AbortSignal) => {
      const [detail, configuration] = await Promise.all([
        invoiceApi.detail(id, signal),
        invoiceApi.configuration(signal),
      ]);
      return { detail, configuration };
    },
    [id],
  );
  const state = useApiResource(`${id}-${revision}`, load);
  function refresh(message = '') {
    setMessage(message);
    setRevision((v) => v + 1);
  }
  async function action() {
    if (!confirmation || !state.data || busy) return;
    setBusy(true);
    setError(undefined);
    try {
      await invoiceApi.action(id, confirmation, state.data.detail.versionToken);
      refresh(confirmation === 'issue' ? t.queued : t.saved);
      setConfirmation(undefined);
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  async function document() {
    setError(undefined);
    try {
      await downloadInvoice(undefined, id);
    } catch {
      setError(0);
    }
  }
  const d = state.data?.detail;
  return (
    <>
      <Link to="/admin/invoices">← {t.title}</Link>
      <h1>{t.detail}</h1>
      {message && <p role="status">{message}</p>}
      {state.error !== undefined ? (
        <PaymentError status={state.error} retry={() => refresh()} />
      ) : !d ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <div className={styles.actions}>
            <Link to={'/admin/quotations/' + d.quotationId}>{t.quote}</Link>
            <Link to={'/admin/requests/' + d.requestId}>{t.customer}</Link>
          </div>
          {d.researchEligible && <p className={styles.notice}>{t.eligible}</p>}
          {d.sandboxResearchEligible && (
            <p className={styles.notice}>{t.sandboxEligible}</p>
          )}
          {d.invoice.status === 'Draft' && (
            <>
              <DraftEditor
                key={d.versionToken}
                detail={d}
                onSaved={() => refresh(t.saved)}
              />
              {state.data!.configuration.blockingReasons.length > 0 && (
                <p role="status">{t.blocked}</p>
              )}
              <div className={styles.actions}>
                <Button
                  disabled={
                    busy || state.data!.configuration.blockingReasons.length > 0
                  }
                  onClick={() => setConfirmation('issue')}
                >
                  {t.issue}
                </Button>
                <Button
                  variant="outline"
                  disabled={busy}
                  onClick={() => setConfirmation('cancel')}
                >
                  {t.cancel}
                </Button>
              </div>
            </>
          )}
          {confirmation && (
            <section className={styles.notice}>
              <p>
                {confirmation === 'issue'
                  ? t.confirmIssue
                  : confirmation === 'cancel'
                    ? t.confirmCancel
                    : t.confirmRevoke}
              </p>
              <div className={styles.actions}>
                <Button disabled={busy} onClick={() => void action()}>
                  {busy ? t.busy : t.confirm}
                </Button>
                <Button
                  variant="outline"
                  disabled={busy}
                  onClick={() => setConfirmation(undefined)}
                >
                  {t.keep}
                </Button>
              </div>
            </section>
          )}
          {error !== undefined && (
            <PaymentError status={error} retry={() => refresh()} />
          )}
          <InvoiceSummary invoice={d.invoice} />
          {d.invoice.issuedAt && (
            <>
              <Button variant="outline" onClick={() => void document()}>
                {t.download}
              </Button>
              <p>{t.documentNote}</p>
              {d.customerLinkActive ? (
                <Button
                  variant="outline"
                  disabled={busy}
                  onClick={() => setConfirmation('revoke-access')}
                >
                  {t.revoke}
                </Button>
              ) : (
                <p>{t.revoked}</p>
              )}
            </>
          )}
          <section className={styles.panel}>
            <h2>{t.attempts}</h2>
            {!d.payments.length ? (
              <p>{t.noAttempts}</p>
            ) : (
              <div className={styles.grid}>
                {d.payments.map((p) => (
                  <article className={styles.line} key={p.id}>
                    <h3>{t.statuses[p.status]}</h3>
                    <p>
                      {p.provider} · {money(p.amountMinorUnits, p.currency)}
                    </p>
                    <p>{p.createdAt.slice(0, 10)}</p>
                    {p.failureReasonCode && <p>{t.failedTitle}</p>}
                    {p.discrepancyCode && (
                      <p role="status">{t.reconciliation}</p>
                    )}
                  </article>
                ))}
              </div>
            )}
          </section>
        </>
      )}
    </>
  );
}
