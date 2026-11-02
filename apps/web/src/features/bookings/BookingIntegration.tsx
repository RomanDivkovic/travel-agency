import { useCallback, useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { Button } from '../../components/ui/Button';
import { useApiResource } from '../../hooks/useApiResource';
import { useMoney } from '../payments/useMoney';
import { bookingApi } from './api';
import { useBookingCopy } from './copy';
import { BookingError, EvidenceForm } from './BookingShared';
import { BookingCards } from './BookingCards';
import styles from './Booking.module.scss';
export function CreateBooking({
  proposalId,
  requestId,
}: {
  proposalId: string;
  requestId: string;
}) {
  const t = useBookingCopy();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const load = useCallback(
    (signal: AbortSignal) =>
      bookingApi.list(new URLSearchParams({ requestId }), signal),
    [requestId],
  );
  const state = useApiResource(requestId, load);
  return (
    <section className={styles.panel}>
      <h2>{t.title}</h2>
      {state.error !== undefined ? (
        <BookingError status={state.error} />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : state.data.items.length ? (
        <BookingCards items={state.data.items} />
      ) : open ? (
        <EvidenceForm
          onClose={() => setOpen(false)}
          onSave={async (evidence) => {
            const r = await bookingApi.create(proposalId, evidence);
            void navigate('/admin/bookings/' + r.id);
          }}
        />
      ) : (
        <Button onClick={() => setOpen(true)}>{t.create}</Button>
      )}
    </section>
  );
}
export function RequestBookings({ requestId }: { requestId: string }) {
  const t = useBookingCopy();
  const money = useMoney();
  const load = useCallback(
    async (signal: AbortSignal) => {
      const list = await bookingApi.list(
        new URLSearchParams({ requestId }),
        signal,
      );
      return Promise.all(
        list.items.map((b) => bookingApi.detail(b.id, signal)),
      );
    },
    [requestId],
  );
  const state = useApiResource(requestId, load);
  return (
    <section className={styles.panel}>
      <h2>{t.title}</h2>
      {state.error !== undefined ? (
        <BookingError status={state.error} />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : !state.data.length ? (
        <p>{t.empty}</p>
      ) : (
        state.data.map((d) => (
          <div key={d.booking.id}>
            <Link to={'/admin/bookings/' + d.booking.id}>
              {d.booking.reference}
            </Link>
            <p>
              {t.statuses[d.booking.status]} · {t.confirmed}:{' '}
              {d.booking.confirmed}/{d.reservations.length}
            </p>
            {Object.entries(
              d.reservations.reduce<Record<string, number>>(
                (totals, r) => ({
                  ...totals,
                  [r.current.currency]:
                    (totals[r.current.currency] ?? 0) +
                    r.current.totalMinorUnits,
                }),
                {},
              ),
            ).map(([currency, total]) => (
              <p key={currency}>
                {t.final}: {money(total, currency)}
              </p>
            ))}
          </div>
        ))
      )}
    </section>
  );
}
export function BookingOverview() {
  const t = useBookingCopy();
  const load = useCallback((s: AbortSignal) => bookingApi.metrics(s), []);
  const state = useApiResource('metrics', load);
  return (
    <section className={styles.panel}>
      <h2>
        <Link to="/admin/bookings">{t.title}</Link>
      </h2>
      {state.error !== undefined ? (
        <BookingError status={state.error} />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <dl className={styles.grid}>
          {[
            [t.statuses.InProgress, state.data.inProgress],
            [t.statuses.PartiallyConfirmed, state.data.partiallyConfirmed],
            [t.statuses.Confirmed, state.data.confirmed],
            [t.failures, state.data.failedReservations],
            [t.upcoming, state.data.upcomingDepartures],
          ].map(([label, value]) => (
            <div key={label}>
              <dt>{label}</dt>
              <dd>{value}</dd>
            </div>
          ))}
        </dl>
      )}
    </section>
  );
}
