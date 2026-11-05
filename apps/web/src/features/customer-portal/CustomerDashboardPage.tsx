import { MessagingOverview } from '../messaging/MessagingIntegration';
import { useCallback, useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { Button } from '../../components/ui/Button';
import { useApiResource } from '../../hooks/useApiResource';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { ApiError } from '../../services/apiClient';
import { customerPortalApi } from './api';
import { useCustomerCopy } from './copy';
import {
  ActivityList,
  CustomerError,
  Field,
  PortalCards,
  PortalLoad,
} from './PortalShared';
import styles from './Customer.module.scss';
export function ClaimRequestForm() {
  const t = useCustomerCopy();
  const [reference, setReference] = useState('');
  const [busy, setBusy] = useState(false),
    [done, setDone] = useState(false);
  const [error, setError] = useState<number>();
  async function submit(e: FormEvent) {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setError(undefined);
    try {
      await customerPortalApi.claim(reference);
      setDone(true);
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className={styles.panel}>
      <h2>{t.claimTitle}</h2>
      <p>{t.claimHint}</p>
      <form className={styles.form} onSubmit={(e) => void submit(e)}>
        <Field
          label={t.reference}
          value={reference}
          required
          maxLength={40}
          onChange={(e) => {
            setReference(e.target.value);
            setDone(false);
          }}
        />
        <Button type="submit" disabled={busy}>
          {busy ? t.saving : t.claimAction}
        </Button>
        {done && <p role="status">{t.claimQueued}</p>}
        {error !== undefined && <CustomerError status={error} />}
      </form>
    </section>
  );
}
export function CustomerDashboardPage() {
  const t = useCustomerCopy();
  usePageMetadata(t.overview);
  const [revision, setRevision] = useState(0);
  const load = useCallback(
    (s: AbortSignal) => customerPortalApi.dashboard(s),
    [],
  );
  const state = useApiResource(String(revision), load);
  const d = state.data;
  return (
    <>
      <h1>{d ? t.welcome + ', ' + d.customer.firstName : t.overview}</h1>
      <Link to="/plan-your-trip">{t.newTrip} →</Link>
      <PortalLoad
        error={state.error}
        loading={!d}
        retry={() => setRevision((v) => v + 1)}
      >
        {d && (
          <>
            <section className={styles.panel}>
              <h2>{t.actions}</h2>
              {d.actionsRequired.length ? (
                <PortalCards items={d.actionsRequired} />
              ) : (
                <p>{t.noActions}</p>
              )}
            </section>
            <section className={styles.panel}>
              <h2>{t.active}</h2>
              {d.requests.length ? (
                <div className={styles.grid}>
                  {d.requests.map((r) => (
                    <article key={r.id} className={styles.card}>
                      <h3>
                        <Link to={'/account/requests/' + r.id}>
                          {r.reference}
                        </Link>
                      </h3>
                      <p>{r.plan.destination}</p>
                      <p>{t.stages[r.journey.stage]}</p>
                      {r.journey.nextAction && (
                        <p>
                          {t.next}: {t.actionLabels[r.journey.nextAction]}
                        </p>
                      )}
                    </article>
                  ))}
                </div>
              ) : (
                <p>{t.empty}</p>
              )}
            </section>
            <section className={styles.panel}>
              <h2>{t.upcoming}</h2>
              <PortalCards items={d.upcomingTrips} />
            </section>
            <section className={styles.panel}>
              <h2>{t.activity}</h2>
              <ActivityList items={d.recentActivity} />
            </section>
          </>
        )}
      </PortalLoad>
      <MessagingOverview />
      <ClaimRequestForm />
    </>
  );
}
