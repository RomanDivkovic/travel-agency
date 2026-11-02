import { useState } from 'react';
import { ResearchOptionEditor } from '../travel-research/ResearchOptionEditor';
import { bookingApi } from './api';
import { useBookingCopy } from './copy';
import type { Booking, Reservation } from './types';
import styles from './Booking.module.scss';
export function ReservationEditor({
  detail,
  r,
  onSaved,
  onClose,
}: {
  detail: Booking;
  r: Reservation;
  onSaved: () => void;
  onClose: () => void;
}) {
  const t = useBookingCopy();
  const [who, setWho] = useState(r.paymentResponsibility ?? '');
  const [status, setStatus] = useState(r.paymentStatus);
  const [arrangement, setArrangement] = useState(r.paymentArrangement);
  return (
    <ResearchOptionEditor
      requestId={detail.booking.requestId}
      kind={r.current.kind}
      option={{
        id: r.id,
        versionToken: detail.versionToken,
        option: r.current,
        notes: '',
        updatedAt: detail.booking.updatedAt,
      }}
      onSaved={onSaved}
      onClose={onClose}
      saveInput={async (option) => {
        await bookingApi.reservation(
          detail.booking.id,
          r.id,
          detail.versionToken,
          'update',
          {
            option,
            paymentResponsibility: who,
            paymentStatus: status,
            paymentArrangement: arrangement,
          },
        );
      }}
      additionalFields={
        <div className={styles.form}>
          <label>
            {t.paymentWho}
            <select
              required
              aria-label={t.paymentWho}
              value={who}
              onChange={(e) => setWho(e.target.value)}
            >
              <option value="">{t.choose}</option>
              {Object.entries(t.who).map(([v, label]) => (
                <option key={v} value={v}>
                  {label}
                </option>
              ))}
            </select>
          </label>
          <label>
            {t.paymentStatus}
            <select
              aria-label={t.paymentStatus}
              value={status}
              onChange={(e) => setStatus(e.target.value)}
            >
              {Object.entries(t.payments).map(([v, label]) => (
                <option key={v} value={v}>
                  {label}
                </option>
              ))}
            </select>
          </label>
          <label>
            {t.paymentArrangement}
            <textarea
              required
              maxLength={1000}
              value={arrangement}
              onChange={(e) => setArrangement(e.target.value)}
            />
          </label>
          <p>{t.reviewHint}</p>
        </div>
      }
    />
  );
}
