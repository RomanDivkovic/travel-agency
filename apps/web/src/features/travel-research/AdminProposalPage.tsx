import { useCallback, useState } from 'react';
import { Link, useParams } from 'react-router';
import { BrandLogo } from '../../components/layout/BrandLogo';
import { Button } from '../../components/ui/Button';
import { useApiResource } from '../../hooks/useApiResource';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { proposalApi, researchApi } from './api';
import { useResearchCopy } from './copy';
import { ProposalActions } from './ProposalActions';
import { ProposalEditor } from './ProposalEditor';
import { ProposalHistory } from './ProposalHistory';
import { ProposalSummary } from './ProposalSummary';
import { ResearchError } from './ResearchShared';
import styles from './Research.module.scss';
export function AdminProposalPage() {
  const { id = '' } = useParams();
  const t = useResearchCopy();
  usePageMetadata(t.proposals);
  const [revision, setRevision] = useState(0);
  const [message, setMessage] = useState('');
  const [preview, setPreview] = useState(true);
  const load = useCallback(
    async (signal: AbortSignal) => {
      const [detail, configuration] = await Promise.all([
        proposalApi.detail(id, signal),
        proposalApi.configuration(signal),
      ]);
      const workspace = await researchApi.workspace(
        detail.requestId,
        1,
        signal,
      );
      return { detail, configuration, workspace };
    },
    [id],
  );
  const state = useApiResource(`${id}-${revision}`, load);
  function refresh(text = t.saved) {
    setMessage(text);
    setRevision((v) => v + 1);
  }
  return (
    <>
      <Link to="/admin/proposals">← {t.proposals}</Link>
      <h1>{t.proposals}</h1>
      {message && <p role="status">{message}</p>}
      {state.error !== undefined ? (
        <ResearchError status={state.error} retry={() => refresh('')} />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <Link
            to={'/admin/requests/' + state.data.detail.requestId + '/research'}
          >
            {t.research}
          </Link>
          {state.data.detail.proposal.status === 'Draft' &&
            state.data.workspace.eligible && (
              <ProposalEditor
                key={'editor-' + state.data.detail.versionToken}
                detail={state.data.detail}
                options={state.data.workspace.options}
                configuration={state.data.configuration}
                onSaved={() => refresh()}
              />
            )}
          {state.data.detail.customerFeedback && (
            <section className={styles.panel}>
              <h2>{t.feedback}</h2>
              <p className={styles.prose}>
                {state.data.detail.customerFeedback}
              </p>
            </section>
          )}
          <ProposalActions
            key={'actions-' + state.data.detail.versionToken}
            detail={state.data.detail}
            configuration={state.data.configuration}
            onSaved={refresh}
          />
          <p>{t.previewHint}</p>
          <Button
            variant="outline"
            aria-expanded={preview}
            onClick={() => setPreview((v) => !v)}
          >
            {t.preview}
          </Button>
          {preview && (
            <section className={styles.preview} aria-label={t.preview}>
              <BrandLogo />
              <ProposalSummary proposal={state.data.detail.proposal} />
            </section>
          )}
          <ProposalHistory id={id} revision={revision} />
        </>
      )}
    </>
  );
}
