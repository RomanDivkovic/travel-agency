import { useCallback } from 'react';
import { Link, NavLink } from 'react-router';
import { messagingApi } from './api';
import { useMessagingCopy } from './copy';
import { useMessaging } from './context';
import { useMessagingResource } from './useMessagingResource';
import { ConversationCards, MessageError, UnreadBadge } from './Shared';
import styles from './Messaging.module.scss';
export function MessagesNavigation({ admin = false }: { admin?: boolean }) {
  const t = useMessagingCopy();
  const { summary } = useMessaging();
  return (
    <NavLink to={(admin ? '/admin' : '/account') + '/messages'}>
      {t.messages} {summary && <UnreadBadge count={summary.unreadCount} />}
    </NavLink>
  );
}
export function MessagingOverview({ admin = false }: { admin?: boolean }) {
  const t = useMessagingCopy();
  const { summary, error, refresh } = useMessaging();
  return (
    <section className={styles.panel}>
      <h2>{t.messages}</h2>
      <Link to={(admin ? '/admin' : '/account') + '/messages'}>
        {t.openInbox}
      </Link>
      {error !== undefined ? (
        <MessageError status={error} retry={refresh} />
      ) : summary ? (
        <>
          <div className={styles.metrics}>
            <div className={styles.metric}>
              <span>{t.unread}</span>
              <strong>{summary.unreadCount}</strong>
            </div>
            <div className={styles.metric}>
              <span>{t.awaiting}</span>
              <strong>{summary.awaitingReply}</strong>
            </div>
            {admin && (
              <div className={styles.metric}>
                <span>{t.high}</span>
                <strong>{summary.highPriority}</strong>
              </div>
            )}
          </div>
          {summary.recent.length ? (
            <ConversationCards compact items={summary.recent} admin={admin} />
          ) : (
            <p>{t.none}</p>
          )}
        </>
      ) : (
        <p>{t.loading}</p>
      )}
    </section>
  );
}
export function ConversationContact({
  id,
  kind,
}: {
  id: string;
  kind: 'request' | 'booking';
}) {
  const t = useMessagingCopy();
  return (
    <p>
      <Link
        to={'/account/messages/new?' + kind + 'Id=' + encodeURIComponent(id)}
      >
        {kind === 'request' ? t.contact : t.askBooking}
      </Link>
    </p>
  );
}
export function RelatedConversations({
  id,
  kind,
}: {
  id: string;
  kind: 'request' | 'booking';
}) {
  const t = useMessagingCopy();
  const load = useCallback(
    (s: AbortSignal) =>
      messagingApi.list(
        true,
        {
          page: 1,
          status: '',
          priority: '',
          search: '',
          unread: false,
          requestId: kind === 'request' ? id : '',
          bookingId: kind === 'booking' ? id : '',
        },
        s,
      ),
    [id, kind],
  );
  const state = useMessagingResource(id + '-' + kind, load);
  return (
    <section className={styles.panel}>
      <h2>{t.messages}</h2>
      <Link to={'/admin/messages?' + kind + 'Id=' + encodeURIComponent(id)}>
        {t.viewRelated}
      </Link>
      {state.error !== undefined ? (
        <MessageError status={state.error} retry={state.refresh} />
      ) : state.data ? (
        state.data.items.length ? (
          <ConversationCards
            compact
            items={state.data.items.slice(0, 3)}
            admin
          />
        ) : (
          <p>{t.empty}</p>
        )
      ) : (
        <p>{t.loading}</p>
      )}
    </section>
  );
}
