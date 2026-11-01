import { useMoney } from './useMoney';
import { useCallback, useState } from 'react';
import { Link } from 'react-router';
import { useApiResource } from '../../hooks/useApiResource';
import { invoiceApi } from './api';
import { usePaymentCopy } from './copy';
import { InvoiceItemSummary } from './InvoiceSummary';
import { PaymentError } from './PaymentError';
import styles from './Payments.module.scss';
export function PaymentOverview() {
  const t = usePaymentCopy();
  const money = useMoney();
  const [revision, setRevision] = useState(0);
  const load = useCallback(
    (signal: AbortSignal) => invoiceApi.summary(signal),
    [],
  );
  const state = useApiResource(String(revision), load);
  return (
    <section className={styles.panel}>
      <h2>{t.overview}</h2>
      <Link to="/admin/invoices">{t.title}</Link>
      {state.error !== undefined ? (
        <PaymentError
          status={state.error}
          retry={() => setRevision((v) => v + 1)}
        />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          {!state.data.currencies.length && <p>{t.noMetrics}</p>}
          <div className={styles.grid}>
            {state.data.currencies.map((c) => (
              <article key={c.currency + c.isSimulated} className={styles.line}>
                <h3>{c.currency}</h3>
                {c.isSimulated && <p>{t.simulation}</p>}
                <dl className={styles.details}>
                  <div>
                    <dt>{t.paid}</dt>
                    <dd>{money(c.paidMinorUnits, c.currency)}</dd>
                  </div>
                  <div>
                    <dt>{t.outstanding}</dt>
                    <dd>{money(c.outstandingMinorUnits, c.currency)}</dd>
                  </div>
                  <div>
                    <dt>{t.pending}</dt>
                    <dd>{c.pendingPayments}</dd>
                  </div>
                  <div>
                    <dt>{t.overdue}</dt>
                    <dd>{c.overdueInvoices}</dd>
                  </div>
                </dl>
              </article>
            ))}
          </div>
          {state.data.recentPayments.length > 0 && (
            <>
              <h3>{t.recent}</h3>
              <div className={styles.grid}>
                {state.data.recentPayments.map((i) => (
                  <article className={styles.line} key={i.id}>
                    <InvoiceItemSummary item={i} />
                    <Link to={'/admin/invoices/' + i.id}>{t.open}</Link>
                  </article>
                ))}
              </div>
            </>
          )}
        </>
      )}
    </section>
  );
}
