import { useCallback, useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { Button } from '../../components/ui/Button';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useApiResource } from '../../hooks/useApiResource';
import { invoiceApi } from './api';
import { usePaymentCopy } from './copy';
import { PaymentError } from './PaymentError';
import { InvoiceItemSummary } from './InvoiceSummary';
import styles from './Payments.module.scss';
export function AdminInvoiceListPage() {
  const t = usePaymentCopy();
  usePageMetadata(t.title);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('');
  const [search, setSearch] = useState('');
  const [applied, setApplied] = useState('');
  const [revision, setRevision] = useState(0);
  const load = useCallback(
    (signal: AbortSignal) => invoiceApi.list(page, status, applied, signal),
    [page, status, applied],
  );
  const state = useApiResource(
    `${page}-${status}-${applied}-${revision}`,
    load,
  );
  function filter(e: FormEvent) {
    e.preventDefault();
    setPage(1);
    setApplied(search);
  }
  return (
    <>
      <h1>{t.title}</h1>
      <form onSubmit={filter} className={styles.form}>
        <label>
          {t.search}
          <input
            value={search}
            maxLength={100}
            onChange={(e) => setSearch(e.target.value)}
          />
        </label>
        <label>
          {t.status}
          <select
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
          >
            <option value="">{t.all}</option>
            {(
              [
                'Draft',
                'Issued',
                'PaymentPending',
                'Paid',
                'Overdue',
                'Cancelled',
              ] as const
            ).map((s) => (
              <option key={s} value={s}>
                {t.statuses[s]}
              </option>
            ))}
          </select>
        </label>
        <Button type="submit">{t.filter}</Button>
      </form>
      {state.error !== undefined ? (
        <PaymentError
          status={state.error}
          retry={() => setRevision((v) => v + 1)}
        />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <div className={styles.grid}>
            {state.data.items.map((i) => (
              <article key={i.id} className={styles.panel}>
                <InvoiceItemSummary item={i} />
                <Link to={'/admin/invoices/' + i.id}>{t.open}</Link>
              </article>
            ))}
          </div>
          {!state.data.items.length && <p>{t.empty}</p>}
          <nav className={styles.actions} aria-label={t.page}>
            <Button
              variant="outline"
              disabled={page === 1}
              onClick={() => setPage((v) => v - 1)}
            >
              {t.previous}
            </Button>
            <span>
              {t.page} {page}
            </span>
            <Button
              variant="outline"
              disabled={page * state.data.pageSize >= state.data.total}
              onClick={() => setPage((v) => v + 1)}
            >
              {t.next}
            </Button>
          </nav>
        </>
      )}
    </>
  );
}
