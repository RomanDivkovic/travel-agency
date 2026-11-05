import { useCallback, useState } from 'react';
import { useApiResource } from '../../hooks/useApiResource';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { customerPortalApi } from './api';
import { useCustomerCopy } from './copy';
import { PortalLoad, PortalCards, CustomerPagination } from './PortalShared';
import type { ResourceKind } from './types';
import styles from './Customer.module.scss';
const statuses: Record<ResourceKind, string[]> = {
  requests: ['Submitted', 'UnderReview', 'AwaitingCustomer', 'Closed'],
  quotations: [
    'Sent',
    'Accepted',
    'Declined',
    'Expired',
    'Superseded',
    'Cancelled',
  ],
  invoices: ['Issued', 'PaymentPending', 'Paid', 'Overdue', 'Cancelled'],
  proposals: [
    'Sent',
    'Approved',
    'Rejected',
    'Expired',
    'Superseded',
    'Cancelled',
  ],
  bookings: [
    'Pending',
    'InProgress',
    'PartiallyConfirmed',
    'Confirmed',
    'Failed',
    'Cancelled',
  ],
};
export function CustomerListPage({ kind }: { kind: ResourceKind }) {
  const t = useCustomerCopy();
  usePageMetadata(t.kinds[kind]!);
  const [page, setPage] = useState(1),
    [status, setStatus] = useState(''),
    [sort, setSort] = useState('updated'),
    [revision, setRevision] = useState(0);
  const load = useCallback(
    (s: AbortSignal) =>
      customerPortalApi.list(
        kind,
        new URLSearchParams({ page: String(page), status, sort }),
        s,
      ),
    [kind, page, status, sort],
  );
  const state = useApiResource(
    `${kind}-${page}-${status}-${sort}-${revision}`,
    load,
  );
  return (
    <>
      <h1>{t.kinds[kind]}</h1>
      <div className={`${styles.form} ${styles.grid}`}>
        <label>
          {t.status}
          <select
            aria-label={t.status}
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
          >
            <option value="">{t.allStatuses}</option>
            {statuses[kind].map((s) => (
              <option key={s} value={s}>
                {t.statuses[s]}
              </option>
            ))}
          </select>
        </label>
        <label>
          {t.sort}
          <select
            aria-label={t.sort}
            value={sort}
            onChange={(e) => {
              setSort(e.target.value);
              setPage(1);
            }}
          >
            <option value="updated">{t.updated}</option>
            <option value="departure">{t.departure}</option>
            <option value="reference">{t.number}</option>
          </select>
        </label>
      </div>
      <PortalLoad
        error={state.error}
        loading={!state.data}
        retry={() => setRevision((v) => v + 1)}
      >
        {state.data && (
          <section className={styles.panel}>
            <h2>{t.records}</h2>
            <PortalCards items={state.data.items} />
            <CustomerPagination {...state.data} onPage={setPage} />
          </section>
        )}
      </PortalLoad>
    </>
  );
}
