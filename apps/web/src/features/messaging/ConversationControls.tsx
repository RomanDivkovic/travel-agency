import { useState, type FormEvent } from 'react';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { messagingApi } from './api';
import { useMessagingCopy } from './copy';
import { MessageError } from './Shared';
import type { ConversationDetail } from './types';
import styles from './Messaging.module.scss';
export function ConversationControls({
  data,
  onSaved,
}: {
  data: ConversationDetail;
  onSaved: () => void;
}) {
  const t = useMessagingCopy();
  const [status, setStatus] = useState(data.conversation.status),
    [priority, setPriority] = useState(data.conversation.priority ?? 'Normal'),
    [busy, setBusy] = useState(false),
    [error, setError] = useState<number>();
  async function save(e: FormEvent, kind: 'status' | 'priority') {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setError(undefined);
    try {
      if (!data.versionToken) throw new ApiError('stale', 409);
      await messagingApi.change(
        data.conversation.id,
        kind,
        kind === 'status' ? status : priority,
        data.versionToken,
      );
      onSaved();
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className={styles.panel}>
      <div className={styles.filters}>
        <form className={styles.form} onSubmit={(e) => void save(e, 'status')}>
          <label>
            {t.status}
            <select
              aria-label={t.status}
              value={status}
              disabled={busy}
              onChange={(e) => setStatus(e.target.value as typeof status)}
            >
              {Object.entries(t.statuses).map(([v, l]) => (
                <option key={v} value={v}>
                  {l}
                </option>
              ))}
            </select>
          </label>
          <Button
            type="submit"
            disabled={busy || status === data.conversation.status}
          >
            {t.save}
          </Button>
        </form>
        <form
          className={styles.form}
          onSubmit={(e) => void save(e, 'priority')}
        >
          <label>
            {t.priority}
            <select
              aria-label={t.priority}
              value={priority}
              disabled={busy}
              onChange={(e) => setPriority(e.target.value as typeof priority)}
            >
              {Object.entries(t.priorities).map(([v, l]) => (
                <option key={v} value={v}>
                  {l}
                </option>
              ))}
            </select>
          </label>
          <Button
            type="submit"
            disabled={busy || priority === data.conversation.priority}
          >
            {t.save}
          </Button>
        </form>
      </div>
      {error !== undefined && <MessageError status={error} />}
    </section>
  );
}
export function ConversationNotifications({
  id,
  enabled,
  onSaved,
}: {
  id: string;
  enabled: boolean;
  onSaved: () => void;
}) {
  const t = useMessagingCopy();
  const [busy, setBusy] = useState(false),
    [error, setError] = useState<number>();
  return (
    <section className={`${styles.panel} ${styles.form}`}>
      <label className={styles.check}>
        <input
          type="checkbox"
          checked={enabled}
          disabled={busy}
          onChange={(e) => {
            setBusy(true);
            setError(undefined);
            void messagingApi
              .preferences(id, e.target.checked)
              .then(onSaved)
              .catch((e: unknown) =>
                setError(e instanceof ApiError ? (e.status ?? 0) : 0),
              )
              .finally(() => setBusy(false));
          }}
        />
        {t.notifications}
      </label>
      <p>{t.emailHint}</p>
      {error !== undefined && <MessageError status={error} />}
    </section>
  );
}
