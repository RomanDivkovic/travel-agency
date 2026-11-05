import { useState } from 'react';
import { Button } from '../../components/ui/Button';
import { useDateFormat } from '../admin/useDateFormat';
import { ApiError } from '../../services/apiClient';
import { messagingApi } from './api';
import { useMessagingCopy } from './copy';
import { MessageError } from './Shared';
import type { ConversationDetail, Message } from './types';
import styles from './Messaging.module.scss';
export function ConversationThread({
  data,
  admin,
}: {
  data: ConversationDetail;
  admin: boolean;
}) {
  const t = useMessagingCopy();
  const date = useDateFormat();
  const [earlier, setEarlier] = useState<Message[]>([]),
    [busy, setBusy] = useState(false),
    [error, setError] = useState<number>();
  const combined = new Map(
    [...earlier, ...data.messages].map((m) => [m.sequence, m]),
  );
  const messages = [...combined.values()].sort(
    (a, b) => a.sequence - b.sequence,
  );
  async function previous() {
    setBusy(true);
    setError(undefined);
    try {
      const d = await messagingApi.detail(
        admin,
        data.conversation.id,
        undefined,
        messages[0]?.sequence,
      );
      setEarlier((rows) => [...d.messages, ...rows]);
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      {(messages[0]?.sequence ?? 1) > 1 && (
        <Button
          variant="outline"
          disabled={busy}
          onClick={() => void previous()}
        >
          {busy ? t.loading : t.earlier}
        </Button>
      )}
      {error !== undefined && <MessageError status={error} />}
      <ol className={styles.thread} aria-label={t.messages}>
        {messages.map((m) => (
          <li
            key={m.id}
            className={`${styles.bubble} ${m.senderRole === 'Administrator' ? styles.agent : styles.customer}`}
          >
            <div className={styles.byline}>
              <strong>
                {m.senderRole === 'Administrator' ? t.agent : t.customer}
                {m.senderName ? ' · ' + m.senderName : ''}
              </strong>
              <time dateTime={m.createdAt}>{date(m.createdAt)}</time>
            </div>
            <p className={styles.body}>{m.body}</p>
          </li>
        ))}
      </ol>
    </>
  );
}
