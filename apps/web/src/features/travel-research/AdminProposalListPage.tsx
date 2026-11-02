import { useCallback, useState, type FormEvent } from 'react';
import { Button } from '../../components/ui/Button';
import { useApiResource } from '../../hooks/useApiResource';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { proposalApi } from './api';
import { useResearchCopy } from './copy';
import { ProposalList } from './ProposalList';
import { ResearchError } from './ResearchShared';
import styles from './Research.module.scss';
export function AdminProposalListPage() {
  const t = useResearchCopy();
  usePageMetadata(t.proposals);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('');
  const [search, setSearch] = useState('');
  const [applied, setApplied] = useState('');
  const [revision, setRevision] = useState(0);
  const load = useCallback(
    (signal: AbortSignal) => proposalApi.list(page, status, applied, signal),
    [page, status, applied],
  );
  const state = useApiResource(
    `${page}-${status}-${applied}-${revision}`,
    load,
  );
  function submit(e: FormEvent) {
    e.preventDefault();
    setApplied(search);
    setPage(1);
  }
  return (
    <>
      <h1>{t.proposals}</h1>
      <form className={styles.form} onSubmit={submit}>
        <label>
          {t.search}
          <input
            value={search}
            maxLength={100}
            onChange={(e) => setSearch(e.target.value)}
          />
        </label>
        <label>
          {t.status}
          <select
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
          >
            <option value="">{t.all}</option>
            {Object.entries(t.statuses).map(([key, label]) => (
              <option key={key} value={key}>
                {label}
              </option>
            ))}
          </select>
        </label>
        <Button type="submit">{t.filter}</Button>
      </form>
      {state.error !== undefined ? (
        <ResearchError
          status={state.error}
          retry={() => setRevision((v) => v + 1)}
        />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <ProposalList data={state.data} onPage={setPage} headingLevel={2} />
      )}
    </>
  );
}
