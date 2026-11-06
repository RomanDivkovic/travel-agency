import { Link } from 'react-router';
import { Button } from '../../components/ui/Button';
import { useDateFormat } from '../admin/useDateFormat';
import { useMessagingCopy } from './copy';
import type { ConversationItem } from './types';
import styles from './Messaging.module.scss';
export function MessageError({
  status,
  retry,
}: {
  status: number;
  retry?: () => void;
}) {
  const t = useMessagingCopy();
  return (
    <div role="alert">
      <p>
        {status === 400
          ? t.invalid
          : status === 404
            ? t.unavailable
            : status === 409
              ? t.conflict
              : status === 412
                ? t.configuration
                : status === 429
                  ? t.limited
                  : t.error}
      </p>
      {retry && (
        <Button variant="outline" onClick={retry}>
          {t.refresh}
        </Button>
      )}
    </div>
  );
}
export function UnreadBadge({ count }: { count: number }) {
  const t = useMessagingCopy();
  return count > 0 ? (
    <span className={styles.badge}>
      {count} {t.unread}
    </span>
  ) : null;
}
export function RelatedLinks({
  item,
  admin,
}: {
  item: ConversationItem;
  admin: boolean;
}) {
  const t = useMessagingCopy();
  const base = admin ? '/admin' : '/account';
  return (
    <div className={styles.actions}>
      {item.requestId && item.requestReference && (
        <Link to={base + '/requests/' + item.requestId}>
          {t.request}: {item.requestReference}
        </Link>
      )}
      {item.bookingId && item.bookingReference && (
        <Link to={base + '/bookings/' + item.bookingId}>
          {t.booking}: {item.bookingReference}
        </Link>
      )}
      {!item.requestId && !item.bookingId && <span>{t.general}</span>}
    </div>
  );
}
export function ConversationCards({
  items,
  admin,
  compact = false,
}: {
  items: ConversationItem[];
  admin: boolean;
  compact?: boolean;
}) {
  const t = useMessagingCopy();
  const date = useDateFormat();
  return (
    <ul className={styles.list}>
      {items.map((c) => (
        <li
          key={c.id}
          className={`${styles.card} ${c.unreadCount ? styles.unread : ''}`}
        >
          <h3>
            <Link to={(admin ? '/admin' : '/account') + '/messages/' + c.id}>
              {c.subject}
            </Link>
          </h3>
          <UnreadBadge count={c.unreadCount} />
          <p>
            <span className={styles.badge}>{t.statuses[c.status]}</span>
            {admin && c.priority && <> · {t.priorities[c.priority]}</>}
          </p>
          {admin && (
            <p>
              {c.customerName} · {c.customerEmail}
            </p>
          )}
          <RelatedLinks item={c} admin={admin} />
          {!compact && <p className={styles.body}>{c.preview}</p>}
          <time dateTime={c.lastMessageAt}>{date(c.lastMessageAt)}</time>
        </li>
      ))}
    </ul>
  );
}
