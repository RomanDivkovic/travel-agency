import { useCallback, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { Button } from '../../components/ui/Button';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useApiResource } from '../../hooks/useApiResource';
import { ApiError } from '../../services/apiClient';
import { adminApi } from '../admin/api';
import { RequestFields } from '../admin/RequestFields';
import { researchApi } from './api';
import { useResearchCopy } from './copy';
import { ResearchError, TextArea } from './ResearchShared';
import { ResearchOptions } from './ResearchOptions';
import { ProposalList } from './ProposalList';
import type { Workspace } from './types';
import styles from './Research.module.scss';
function Notes({
  id,
  workspace: w,
  onSaved,
}: {
  id: string;
  workspace: Workspace;
  onSaved: () => void;
}) {
  const t = useResearchCopy();
  const [notes, setNotes] = useState(w.notes);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  async function save(e: FormEvent) {
    e.preventDefault();
    if (busy || !w.versionToken) return;
    setBusy(true);
    setError(undefined);
    try {
      await researchApi.notes(id, w.versionToken, notes);
      onSaved();
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className={styles.panel}>
      <h2>{t.notes}</h2>
      <p>{t.private}</p>
      <form className={styles.form} onSubmit={(e) => void save(e)}>
        <TextArea
          label={t.notes}
          value={notes}
          onChange={setNotes}
          maxLength={4000}
        />
        <Button type="submit" disabled={busy}>
          {busy ? t.saving : t.saveNotes}
        </Button>
      </form>
      {error !== undefined && <ResearchError status={error} />}
    </section>
  );
}
export function AdminResearchPage() {
  const { id = '' } = useParams();
  const t = useResearchCopy();
  usePageMetadata(t.research);
  const navigate = useNavigate();
  const [revision, setRevision] = useState(0);
  const [page, setPage] = useState(1);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  const [message, setMessage] = useState('');
  const load = useCallback(
    async (signal: AbortSignal) => {
      const [request, workspace] = await Promise.all([
        adminApi.detail(id, signal),
        researchApi.workspace(id, page, signal),
      ]);
      return { request, workspace };
    },
    [id, page],
  );
  const state = useApiResource(`${id}-${page}-${revision}`, load);
  function refresh() {
    setRevision((v) => v + 1);
    setMessage('');
  }
  function saved() {
    setRevision((v) => v + 1);
    setMessage(t.saved);
  }
  async function action(create = false) {
    if (busy) return;
    setBusy(true);
    setError(undefined);
    try {
      if (create) {
        const p = await researchApi.createProposal(id);
        void navigate('/admin/proposals/' + p.id + '/edit');
      } else {
        await researchApi.start(id);
        saved();
      }
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  const w = state.data?.workspace;
  return (
    <>
      <Link to={'/admin/requests/' + id}>← {t.back}</Link>
      <h1>{t.research}</h1>
      {message && <p role="status">{message}</p>}
      {state.error !== undefined ? (
        <ResearchError status={state.error} retry={refresh} />
      ) : !w || !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <p className={styles.badge}>
            {w.status === 'InProgress' ? t.inProgress : t.notStarted}
          </p>
          {!w.eligible && <p className={styles.notice}>{t.eligibility}</p>}
          {error !== undefined && (
            <ResearchError status={error} retry={refresh} />
          )}
          <div className={styles.workspace}>
            <aside className={styles.requirements} aria-label={t.requirements}>
              <h2>{t.requirements}</h2>
              <p>{state.data.request.referenceNumber}</p>
              <RequestFields request={state.data.request} />
            </aside>
            <div>
              {w.status === 'NotStarted' ? (
                <Button
                  disabled={!w.eligible || busy}
                  onClick={() => void action()}
                >
                  {t.start}
                </Button>
              ) : (
                <>
                  <ResearchOptions
                    requestId={id}
                    options={w.options}
                    eligible={w.eligible}
                    onSaved={saved}
                    onRefresh={refresh}
                  />
                  {w.eligible && (
                    <Notes
                      key={w.versionToken}
                      id={id}
                      workspace={w}
                      onSaved={saved}
                    />
                  )}
                  <section className={styles.panel}>
                    <h2>{t.proposals}</h2>
                    {!w.proposals.totalCount && (
                      <Button
                        disabled={!w.eligible || busy}
                        onClick={() => void action(true)}
                      >
                        {t.create}
                      </Button>
                    )}
                    <ProposalList data={w.proposals} onPage={setPage} />
                  </section>
                </>
              )}
            </div>
          </div>
        </>
      )}
    </>
  );
}
