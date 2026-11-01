import { useState, type FormEvent } from 'react';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { adminApi } from './api';
import { useAdminCopy } from './copy';
import type { RequestDetail, RequestStatus } from './model';
import styles from './Admin.module.scss';
export function RequestActions({
  request,
  onSaved,
  onRefresh,
}: {
  request: RequestDetail;
  onSaved: (kind: 'status' | 'note') => void;
  onRefresh: () => void;
}) {
  const t = useAdminCopy();
  const [status, setStatus] = useState<RequestStatus | ''>(
    request.allowedStatuses[0] ?? '',
  );
  const [note, setNote] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  async function save(e: FormEvent, kind: 'status' | 'note') {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setError(undefined);
    try {
      if (kind === 'status' && status)
        await adminApi.status(request.id, status, request.version);
      else await adminApi.addNote(request.id, note);
      if (kind === 'note') setNote('');
      onSaved(kind);
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <div className={styles.detailGrid}>
      <section className={styles.panel}>
        <h2>{t.changeStatus}</h2>
        <p>{t.noBooking}</p>
        {status ? (
          <form onSubmit={(e) => void save(e, 'status')}>
            <label>
              {t.status}
              <select
                aria-label={t.status}
                value={status}
                disabled={busy}
                onChange={(e) => setStatus(e.target.value as RequestStatus)}
              >
                {request.allowedStatuses.map((s) => (
                  <option key={s} value={s}>
                    {t.statuses[s]}
                  </option>
                ))}
              </select>
            </label>
            <Button type="submit" disabled={busy}>
              {busy ? t.saving : t.saveStatus}
            </Button>
          </form>
        ) : (
          <p>{t.noTransitions}</p>
        )}
      </section>
      <section className={styles.panel}>
        <h2>{t.notes}</h2>
        <p>{t.private}</p>
        <form onSubmit={(e) => void save(e, 'note')}>
          <label>
            {t.note}
            <textarea
              required
              minLength={1}
              maxLength={4000}
              value={note}
              onChange={(e) => setNote(e.target.value)}
              disabled={busy}
              aria-describedby="note-limit"
            />
          </label>
          <p id="note-limit">{t.noteLimit}</p>
          <Button type="submit" disabled={busy || !note.trim()}>
            {busy ? t.saving : t.addNote}
          </Button>
        </form>
      </section>
      {error !== undefined ? (
        <div role="alert">
          <p>
            {error === 409
              ? t.conflict
              : error === 400
                ? t.invalid
                : error === 403
                  ? t.forbidden
                  : t.error}
          </p>
          {error === 409 ? (
            <Button onClick={onRefresh}>{t.refresh}</Button>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
