import { useId, useRef, useState, type FormEvent } from 'react';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { useMessagingCopy } from './copy';
import { MessageError } from './Shared';
import styles from './Messaging.module.scss';
export function MessageComposer({
  maxLength,
  onSend,
}: {
  maxLength: number;
  onSend: (body: string, clientId: string) => Promise<void>;
}) {
  const t = useMessagingCopy();
  const id = useId(),
    hint = useId();
  const [body, setBody] = useState(''),
    [clientId, setClientId] = useState(() => crypto.randomUUID()),
    [busy, setBusy] = useState(false),
    [sent, setSent] = useState(false),
    [error, setError] = useState<number>();
  const pending = useRef(false);
  async function submit(e: FormEvent) {
    e.preventDefault();
    if (pending.current) return;
    if (!body.trim()) {
      setError(400);
      return;
    }
    pending.current = true;
    setBusy(true);
    setSent(false);
    setError(undefined);
    try {
      await onSend(body, clientId);
      setBody('');
      setClientId(crypto.randomUUID());
      setSent(true);
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      pending.current = false;
      setBusy(false);
    }
  }
  return (
    <section className={styles.panel}>
      <h2>{t.reply}</h2>
      <form className={styles.form} onSubmit={(e) => void submit(e)}>
        <label htmlFor={id}>
          {t.message}
          <textarea
            id={id}
            required
            maxLength={maxLength}
            rows={6}
            value={body}
            disabled={busy}
            aria-describedby={hint}
            onChange={(e) => {
              setBody(e.target.value);
              setClientId(crypto.randomUUID());
              setSent(false);
            }}
          />
        </label>
        <p id={hint}>
          {maxLength} {t.characters}
        </p>
        <Button type="submit" disabled={busy || !body.trim()}>
          {busy ? t.sending : t.send}
        </Button>
        {sent && <p role="status">{t.sent}</p>}
        {error !== undefined && <MessageError status={error} />}
      </form>
    </section>
  );
}
