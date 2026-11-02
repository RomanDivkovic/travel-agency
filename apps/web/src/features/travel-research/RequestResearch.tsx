import { useCallback, useState } from 'react';
import { Link } from 'react-router';
import { useApiResource } from '../../hooks/useApiResource';
import { researchApi } from './api';
import { useResearchCopy } from './copy';
import { ProposalList } from './ProposalList';
import { ResearchError } from './ResearchShared';
import styles from './Research.module.scss';
export function RequestResearch({ requestId }: { requestId: string }) {
  const t = useResearchCopy();
  const [page, setPage] = useState(1);
  const [revision, setRevision] = useState(0);
  const load = useCallback(
    (signal: AbortSignal) => researchApi.workspace(requestId, page, signal),
    [requestId, page],
  );
  const state = useApiResource(`${requestId}-${page}-${revision}`, load);
  return (
    <section className={styles.panel}>
      <h2>{t.research}</h2>
      {state.error !== undefined ? (
        <ResearchError
          status={state.error}
          retry={() => setRevision((v) => v + 1)}
        />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <p>
            {state.data.status === 'InProgress' ? t.inProgress : t.notStarted}
          </p>
          {!state.data.eligible && <p>{t.eligibility}</p>}
          <Link to={'/admin/requests/' + requestId + '/research'}>
            {t.research} · {state.data.options.length} {t.options.toLowerCase()}
          </Link>
          <h3>{t.history}</h3>
          <ProposalList data={state.data.proposals} onPage={setPage} />
        </>
      )}
    </section>
  );
}
