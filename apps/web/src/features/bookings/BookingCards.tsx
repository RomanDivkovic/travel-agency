import { Link } from 'react-router';
import { useDateFormat } from '../admin/useDateFormat';
import { useBookingCopy } from './copy';
import type { BookingItem } from './types';
import styles from './Booking.module.scss';
export function BookingCards({ items }: { items: BookingItem[] }) {
  const t = useBookingCopy();
  const date = useDateFormat();
  return (
    <div className={styles.grid}>
      {items.map((b) => (
        <article key={b.id}>
          <h3>
            <Link to={'/admin/bookings/' + b.id}>{b.reference}</Link>
          </h3>
          <p>
            {b.customerName} · {b.destination}
          </p>
          <p>
            {b.departure} – {b.return}
          </p>
          <p className={styles.status}>{t.statuses[b.status]}</p>
          <p>
            {t.confirmed}: {b.confirmed} · {t.pending}: {b.pending} ·{' '}
            {t.failures}: {b.failed}
          </p>
          <p>
            {t.updated}: {date(b.updatedAt)}
          </p>
        </article>
      ))}
    </div>
  );
}
