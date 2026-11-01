import { useDateFormat } from './useDateFormat';
import { useCallback, useState } from 'react';
import { adminApi } from './api';
import { useAdminResource } from './useAdminResource';
import { useAdminCopy } from './copy';
import { ErrorState, Pagination } from './AdminShared';
import styles from './Admin.module.scss';
export function RequestTimeline({
  id,
  revision,
  kind,
}: {
  id: string;
  revision: number;
  kind: 'notes' | 'activity';
}) {
  const t = useAdminCopy();
  const date = useDateFormat();
  const [page, setPage] = useState(1);
  const [retry, setRetry] = useState(0);
  const load = useCallback(
    async (signal: AbortSignal) => {
      if (kind === 'notes') {
        const r = await adminApi.notes(id, page, signal);
        return {
          ...r,
          items: r.items.map((n) => ({
            id: n.id,
            content: n.content,
            author: n.author,
            createdAt: n.createdAt,
            type: '' as string,
            previousValue: null,
            newValue: null,
          })),
        };
      }
      const r = await adminApi.activity(id, page, signal);
      return { ...r, items: r.items.map((a) => ({ ...a, content: '' })) };
    },
    [id, page, kind],
  );
  const state = useAdminResource(
    `${id}-${kind}-${revision}-${page}-${retry}`,
    load,
  );
  return (
    <section className={styles.panel}>
      <h2>{kind === 'notes' ? t.notes : t.history}</h2>
      {state.error !== undefined ? (
        <ErrorState status={state.error} retry={() => setRetry((v) => v + 1)} />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          {state.data.items.length ? (
            <ol className={styles.timeline}>
              {state.data.items.map((item) => (
                <li key={item.id}>
                  <p>
                    {item.type === 'Submitted'
                      ? t.requestSubmitted
                      : item.type === 'NoteAdded'
                        ? t.noteAdded
                        : item.type === 'StatusChanged'
                          ? `${t.statusChanged}: ${item.previousValue ? t.statuses[item.previousValue] : ''} → ${item.newValue ? t.statuses[item.newValue] : ''}`
                          : null}
                  </p>
                  {item.content ? (
                    <p className={styles.prose}>{item.content}</p>
                  ) : null}
                  <small>
                    {item.author ?? t.customer} ·{' '}
                    <time dateTime={item.createdAt}>
                      {date(item.createdAt)}
                    </time>
                  </small>
                </li>
              ))}
            </ol>
          ) : (
            <p>{kind === 'notes' ? t.noNotes : t.noHistory}</p>
          )}
          {state.data.totalPages > 1 ? (
            <Pagination {...state.data} onPage={setPage} />
          ) : null}
        </>
      )}
    </section>
  );
}
