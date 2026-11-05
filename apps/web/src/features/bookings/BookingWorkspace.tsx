import { useCallback, useState } from 'react';
import { Link, useParams } from 'react-router';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { useApiResource } from '../../hooks/useApiResource';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useDateFormat } from '../admin/useDateFormat';
import { Pagination } from '../admin/AdminShared';
import { useMoney } from '../payments/useMoney';
import { ResearchOptionSummary } from '../travel-research/ResearchOptionSummary';
import { ProposalSummary } from '../travel-research/ProposalSummary';
import { TextArea } from '../travel-research/ResearchShared';
import { bookingApi } from './api';
import { useDocumentCopy } from '../travel-documents/copy';
import { useBookingCopy } from './copy';
import { BookingError, ConfirmAction } from './BookingShared';
import { ReservationActions } from './ReservationActions';
import type { Booking, Reservation } from './types';
import styles from './Booking.module.scss';
function ReservationCard({
  detail,
  r,
  onSaved,
}: {
  detail: Booking;
  r: Reservation;
  onSaved: () => void;
}) {
  const t = useBookingCopy();
  const money = useMoney();
  const [checkedAt] = useState(() => Date.now());
  return (
    <article className={styles.panel}>
      <h2>{r.current.title}</h2>
      <p className={styles.status}>{t.reservations[r.status]}</p>
      {r.requiresReview && (
        <p className={styles.notice}>
          {r.reapprovalRecorded ? t.reviewed : t.review}
        </p>
      )}
      <p>
        {t.original}: {money(r.approved.totalMinorUnits, r.approved.currency)}
      </p>
      <p>
        {t.final}: {money(r.current.totalMinorUnits, r.current.currency)}
      </p>
      {r.approved.currency === r.current.currency && (
        <p>
          {t.difference}:{' '}
          {money(
            r.current.totalMinorUnits - r.approved.totalMinorUnits,
            r.current.currency,
          )}
        </p>
      )}
      <p>
        {t.paymentWho}:{' '}
        {r.paymentResponsibility
          ? t.who[r.paymentResponsibility as keyof typeof t.who]
          : t.choose}
      </p>
      <p>
        {t.paymentStatus}:{' '}
        {t.payments[r.paymentStatus as keyof typeof t.payments]}
      </p>
      <p>{r.paymentArrangement}</p>
      <ResearchOptionSummary option={r.current} />
      <ul className={styles.checklist}>
        {[
          [
            t.checkPrice,
            !!r.current.availabilityCheckedAt &&
              Date.parse(r.current.availabilityCheckedAt) >=
                checkedAt - 86400000 &&
              Date.parse(r.current.priceCheckedAt) >= checkedAt - 86400000,
          ],
          [t.setPayment, !!r.paymentResponsibility],
          [t.reserve, r.status === 'Confirmed'],
          [t.record, r.status === 'Confirmed'],
        ].map(([label, done]) => (
          <li key={String(label)}>
            {done ? '✓' : '○'} {label}
          </li>
        ))}
      </ul>
      {r.supplierReference && (
        <p>
          {t.supplierReference}: {r.supplierReference}
        </p>
      )}
      {r.confirmationEvidence && (
        <p>
          {t.supplierEvidence}: {r.confirmationEvidence}
        </p>
      )}
      {r.current.kind === 'Flight' && (
        <p>
          {t.ticketing}:{' '}
          {r.ticketingStatus === 'Issued' ? t.issued : t.notTicketed} ·{' '}
          {t.passengers}:{' '}
          {detail.proposal.trip.adults + detail.proposal.trip.children}
        </p>
      )}
      {r.failureReason && (
        <p role="status">
          {t.fail}: {r.failureReason}
        </p>
      )}
      {r.cancelledAt && (
        <p>
          {t.cancellation}: {r.cancellationReason} · {r.cancellationReference}
          {r.cancellationChargeMinorUnits !== null
            ? ' · ' + money(r.cancellationChargeMinorUnits, r.current.currency)
            : ''}
        </p>
      )}
      <ReservationActions detail={detail} r={r} onSaved={onSaved} />
    </article>
  );
}
function Notes({ detail, onSaved }: { detail: Booking; onSaved: () => void }) {
  const t = useBookingCopy();
  const [notes, setNotes] = useState(detail.notes);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  return (
    <form
      className={`${styles.panel} ${styles.form}`}
      onSubmit={(e) => {
        e.preventDefault();
        setBusy(true);
        setError(undefined);
        void bookingApi
          .notes(detail.booking.id, detail.versionToken, notes)
          .then(onSaved)
          .catch((e: unknown) =>
            setError(e instanceof ApiError ? (e.status ?? 0) : 0),
          )
          .finally(() => setBusy(false));
      }}
    >
      <h2>{t.notes}</h2>
      <TextArea
        label={t.notes}
        value={notes}
        onChange={setNotes}
        maxLength={4000}
      />
      <Button type="submit" disabled={busy}>
        {busy ? t.saving : t.save}
      </Button>
      {error !== undefined && <BookingError status={error} />}
    </form>
  );
}
function History({ id, revision }: { id: string; revision: number }) {
  const t = useBookingCopy();
  const date = useDateFormat();
  const [page, setPage] = useState(1);
  const load = useCallback(
    (s: AbortSignal) => bookingApi.history(id, page, s),
    [id, page],
  );
  const state = useApiResource(`${id}-${page}-${revision}`, load);
  return (
    <section className={styles.panel}>
      <h2>{t.history}</h2>
      {state.error !== undefined ? (
        <BookingError status={state.error} />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <ol>
            {state.data.items.map((e) => (
              <li key={e.id}>
                <p>
                  {t.events[e.type as keyof typeof t.events] ?? e.type} ·{' '}
                  {e.actor} · {date(e.createdAt)}
                </p>
                <details>
                  <summary>{t.snapshot}</summary>
                  <pre className={styles.history}>
                    {JSON.stringify(
                      JSON.parse(e.snapshotJson) as unknown,
                      null,
                      2,
                    )}
                  </pre>
                </details>
              </li>
            ))}
          </ol>
          <Pagination {...state.data} onPage={setPage} />
        </>
      )}
    </section>
  );
}
export function AdminBookingDetailPage() {
  const docs = useDocumentCopy();
  const { id = '' } = useParams();
  const t = useBookingCopy();
  usePageMetadata(t.workspace);
  const [revision, setRevision] = useState(0);
  const [message, setMessage] = useState('');
  const load = useCallback((s: AbortSignal) => bookingApi.detail(id, s), [id]);
  const state = useApiResource(`${id}-${revision}`, load);
  const money = useMoney();
  function refresh(saved = false) {
    setMessage(saved ? t.saved : '');
    setRevision((v) => v + 1);
  }
  const d = state.data;
  return (
    <>
      <Link to="/admin/bookings">← {t.title}</Link>
      <h1>{t.workspace}</h1>
      {message && <p role="status">{message}</p>}
      {state.error !== undefined ? (
        <BookingError status={state.error} retry={() => refresh()} />
      ) : !d ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <section className={styles.panel}>
            <h2>{d.booking.reference}</h2>
            <nav className={styles.actions}>
              <Link to={'/admin/bookings/' + d.booking.id + '/documents'}>
                {docs.manageDocuments}
              </Link>
              <Link to={'/admin/bookings/' + d.booking.id + '/itinerary'}>
                {docs.manageItinerary}
              </Link>
            </nav>
            <p>
              {d.booking.customerName} · {d.booking.destination} ·{' '}
              {d.booking.departure} – {d.booking.return}
            </p>
            <p className={styles.status}>{t.statuses[d.booking.status]}</p>
            <p>
              {t.confirmed}: {d.booking.confirmed}/{d.reservations.length} ·{' '}
              {t.failures}: {d.booking.failed}
            </p>
            <progress
              aria-label={t.confirmed}
              value={d.booking.confirmed}
              max={d.reservations.length}
            />
            <div className={styles.actions}>
              <Link to={'/admin/requests/' + d.booking.requestId}>
                {t.customer}
              </Link>
              <Link to={'/admin/proposals/' + d.booking.proposalId}>
                {t.preview}
              </Link>
            </div>
            <p>
              {t.selection}:{' '}
              {
                d.proposal.alternatives.find(
                  (a) => a.id === d.proposal.selectedAlternativeId,
                )?.title
              }
            </p>
            {!d.eligible && <p role="alert">{t.blocked}</p>}
            <h3>{t.final}</h3>
            {Object.entries(
              d.reservations.reduce<Record<string, number>>(
                (sum, r) => ({
                  ...sum,
                  [r.current.currency]:
                    (sum[r.current.currency] ?? 0) + r.current.totalMinorUnits,
                }),
                {},
              ),
            ).map(([c, n]) => (
              <p key={c}>{money(n, c)}</p>
            ))}
            <p>{t.separate}</p>
          </section>
          <section className={styles.panel}>
            <h2>{t.checklist}</h2>
            <p>{t.checklistHint}</p>
            <ul>
              {d.reservations.map((r) => (
                <li key={r.id}>
                  {r.current.title}: {t.reservations[r.status]}
                </li>
              ))}
            </ul>
          </section>
          {d.reservations.map((r) => (
            <ReservationCard
              key={r.id + '-' + d.versionToken}
              detail={d}
              r={r}
              onSaved={() => refresh(true)}
            />
          ))}
          {d.booking.status === 'Confirmed' && d.eligible && (
            <ConfirmAction
              label={t.send}
              hint={t.sendHint}
              onConfirm={async () => {
                await bookingApi.send(id, d.versionToken);
                refresh(true);
              }}
            />
          )}
          <section className={styles.panel}>
            <h2>{t.notifications}</h2>
            <p>{t.sendHint}</p>
            <ul>
              {d.notifications.map((n) => (
                <li key={n.kind}>
                  {n.kind.startsWith('booking-confirmed-')
                    ? t.confirmationMail
                    : t.failureMail}
                  :{' '}
                  {n.sentAt ? t.delivered : n.deadLetterAt ? t.dead : t.queued}{' '}
                  ({t.attempts}: {n.attempts})
                </li>
              ))}
            </ul>
          </section>
          <Notes
            key={'notes-' + d.versionToken}
            detail={d}
            onSaved={() => refresh(true)}
          />
          <details className={styles.panel}>
            <summary>{t.preview}</summary>
            <ProposalSummary proposal={d.proposal} />
          </details>
          <History id={id} revision={revision} />
        </>
      )}
    </>
  );
}
