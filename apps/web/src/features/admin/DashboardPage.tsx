import { BookingOverview } from '../bookings/BookingIntegration';
import { PaymentOverview } from '../payments/PaymentOverview';
import { useCallback, useState } from 'react';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { adminApi } from './api';
import { useAdminResource } from './useAdminResource';
import { useAdminCopy } from './copy';
import { ErrorState, RequestCards } from './AdminShared';
import styles from './Admin.module.scss';
export function DashboardPage() {
  const t = useAdminCopy();
  usePageMetadata(t.dashboard);
  const [revision, setRevision] = useState(0);
  const load = useCallback(
    (signal: AbortSignal) => adminApi.dashboard(signal),
    [],
  );
  const state = useAdminResource(String(revision), load);
  return (
    <>
      <p className={styles.eyebrow}>{t.admin}</p>
      <h1>{t.dashboard}</h1>
      {state.error !== undefined ? (
        <ErrorState
          status={state.error}
          retry={() => setRevision((v) => v + 1)}
        />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <div className={styles.metrics}>
            {[
              [t.total, state.data.total],
              [t.statuses.Submitted, state.data.submitted],
              [t.statuses.UnderReview, state.data.underReview],
              [t.statuses.AwaitingCustomer, state.data.awaitingCustomer],
              [t.closed, state.data.closed],
            ].map(([label, value]) => (
              <div className={styles.metric} key={label}>
                <span>{label}</span>
                <strong>{value}</strong>
              </div>
            ))}
          </div>
          <PaymentOverview />
          <BookingOverview />
          <h2>{t.recent}</h2>
          <RequestCards nested items={state.data.recentlyUpdated} />
        </>
      )}
    </>
  );
}
