import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router';
import { Button } from '../../components/ui/Button';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useDateFormat } from '../admin/useDateFormat';
import { messagingApi } from './api';
import { useMessaging } from './context';
import { useMessagingCopy } from './copy';
import { useMessagingResource } from './useMessagingResource';
import { MessageError, RelatedLinks } from './Shared';
import { MessageComposer } from './MessageComposer';
import { ConversationThread } from './ConversationThread';
import {
  ConversationControls,
  ConversationNotifications,
} from './ConversationControls';
import styles from './Messaging.module.scss';
function Conversation({ id, admin }: { id: string; admin: boolean }) {
  const t = useMessagingCopy();
  const summary = useMessaging();
  usePageMetadata(t.messages);
  const date = useDateFormat();
  const load = useCallback(
    (s: AbortSignal) => messagingApi.detail(admin, id, s),
    [admin, id],
  );
  const state = useMessagingResource(id + '-' + admin, load);
  const d = state.data;
  const [readFailed, setReadFailed] = useState(false),
    [saved, setSaved] = useState(false);
  const marked = useRef(0);
  const through = d?.messages.at(-1)?.sequence ?? 0;
  const refreshSummary = summary.refresh;
  const mark = useCallback(() => {
    if (
      !through ||
      document.visibilityState !== 'visible' ||
      marked.current >= through
    )
      return;
    marked.current = through;
    void messagingApi
      .read(admin, id, through)
      .then(() => {
        setReadFailed(false);
        refreshSummary();
      })
      .catch(() => {
        marked.current = 0;
        setReadFailed(true);
      });
  }, [through, admin, id, refreshSummary]);
  useEffect(() => {
    const timer = window.setTimeout(mark, 0);
    document.addEventListener('visibilitychange', mark);
    return () => {
      window.clearTimeout(timer);
      document.removeEventListener('visibilitychange', mark);
    };
  }, [mark]);
  const refresh = () => {
    state.refresh();
    summary.refresh();
    setSaved(true);
  };
  return (
    <>
      <Link to={(admin ? '/admin' : '/account') + '/messages'}>← {t.back}</Link>
      <h1>{d?.conversation.subject ?? t.messages}</h1>
      <Button variant="outline" onClick={state.refresh}>
        {t.refresh}
      </Button>
      <p className={styles.notice}>{t.formal}</p>
      {saved && <p role="status">{t.saved}</p>}
      {state.error !== undefined && !d ? (
        <MessageError status={state.error} retry={state.refresh} />
      ) : !d ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          {state.error !== undefined && (
            <MessageError status={state.error} retry={state.refresh} />
          )}
          <p>
            <span className={styles.badge}>
              {t.statuses[d.conversation.status]}
            </span>
            {admin && d.conversation.priority && (
              <> · {t.priorities[d.conversation.priority]}</>
            )}
          </p>
          {admin && (
            <p>
              {d.conversation.customerName} · {d.conversation.customerEmail}
            </p>
          )}
          <RelatedLinks item={d.conversation} admin={admin} />
          {admin && (
            <ConversationControls
              key={d.versionToken}
              data={d}
              onSaved={refresh}
            />
          )}
          <ConversationThread data={d} admin={admin} />
          {readFailed && (
            <div role="alert">
              <p>{t.readError}</p>
              <Button onClick={() => void mark()}>{t.markRead}</Button>
            </div>
          )}
          {d.conversation.status === 'Closed' ? (
            <section className={styles.notice}>
              <p>{t.closed}</p>
              {!admin && (
                <Link to="/account/messages/new">{t.newConversation}</Link>
              )}
            </section>
          ) : (
            <MessageComposer
              maxLength={d.maxMessageLength}
              onSend={async (body, key) => {
                await messagingApi.send(admin, id, body, key);
                state.refresh();
                summary.refresh();
              }}
            />
          )}
          {!admin && (
            <ConversationNotifications
              id={id}
              enabled={d.emailNotifications}
              onSaved={refresh}
            />
          )}
          {admin && d.history && (
            <details className={styles.panel}>
              <summary>{t.history}</summary>
              <ol className={styles.history}>
                {d.history.map((e) => (
                  <li key={e.id}>
                    {t.events[e.type] ?? e.type}
                    {e.previous
                      ? ' · ' +
                        (t.statuses[e.previous] ??
                          t.priorities[e.previous] ??
                          e.previous)
                      : ''}
                    {e.current
                      ? ' → ' +
                        (t.statuses[e.current] ??
                          t.priorities[e.current] ??
                          e.current)
                      : ''}{' '}
                    · <time dateTime={e.createdAt}>{date(e.createdAt)}</time>
                  </li>
                ))}
              </ol>
            </details>
          )}
        </>
      )}
    </>
  );
}
export function ConversationPage({ admin = false }: { admin?: boolean }) {
  const { id = '' } = useParams();
  return <Conversation key={id + '-' + admin} id={id} admin={admin} />;
}
