import { useCallback, useState } from 'react';
import { Button } from '../../components/ui/Button';
import { useApiResource } from '../../hooks/useApiResource';
import { proposalApi } from './api';
import { useResearchCopy } from './copy';
import { ResearchError } from './ResearchShared';
import styles from './Research.module.scss';
export function ProposalHistory({
  id,
  revision,
}: {
  id: string;
  revision: number;
}) {
  const t = useResearchCopy();
  const [page, setPage] = useState(1);
  const [retry, setRetry] = useState(0);
  const load = useCallback(
    (signal: AbortSignal) => proposalApi.history(id, page, signal),
    [id, page],
  );
  const state = useApiResource(`${id}-${revision}-${page}-${retry}`, load);
  return (
    <section className={styles.panel}>
      <h2>{t.history}</h2>
      {state.error !== undefined ? (
        <ResearchError
          status={state.error}
          retry={() => setRetry((v) => v + 1)}
        />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <ol>
            {state.data.items.map((a) => (
              <li key={a.id}>
                <p>{t.events[a.type as keyof typeof t.events] ?? a.type}</p>
                <small>
                  {a.author ?? (a.type === 'ProposalExpired' ? t.system : '—')}{' '}
                  ·{' '}
                  <time dateTime={a.createdAt}>
                    {a.createdAt.replace('T', ' ').replace('Z', ' UTC')}
                  </time>
                </small>
              </li>
            ))}
          </ol>
          {state.data.totalPages > 1 && (
            <nav className={styles.actions} aria-label={t.page}>
              <Button
                disabled={page <= 1}
                onClick={() => setPage((v) => v - 1)}
              >
                {t.previous}
              </Button>
              <span>
                {t.page} {page} / {state.data.totalPages}
              </span>
              <Button
                disabled={page >= state.data.totalPages}
                onClick={() => setPage((v) => v + 1)}
              >
                {t.next}
              </Button>
            </nav>
          )}
        </>
      )}
    </section>
  );
}
