import { useState } from 'react';
import { useNavigate } from 'react-router';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { proposalApi } from './api';
import { useResearchCopy } from './copy';
import { ResearchError } from './ResearchShared';
import type { AdminProposal, ProposalConfiguration } from './types';
import styles from './Research.module.scss';
export function ProposalActions({
  detail: d,
  configuration,
  onSaved,
}: {
  detail: AdminProposal;
  configuration: ProposalConfiguration;
  onSaved: (message: string) => void;
}) {
  const t = useResearchCopy();
  const navigate = useNavigate();
  const [confirm, setConfirm] = useState<
    'send' | 'revise' | 'cancel' | 'revoke-access'
  >();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  async function action() {
    if (!confirm || busy) return;
    setBusy(true);
    setError(undefined);
    try {
      const result = await proposalApi.action(d.id, confirm, d.versionToken);
      if (confirm === 'revise' && result) {
        void navigate('/admin/proposals/' + result.id + '/edit');
      } else onSaved(confirm === 'send' ? t.queued : t.saved);
      setConfirm(undefined);
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  const draft = d.proposal.status === 'Draft';
  return (
    <section className={styles.panel}>
      {!draft && <p>{t.immutable}</p>}
      {draft && configuration.blockingReasons.length > 0 && <p>{t.blocked}</p>}
      <div className={styles.actions}>
        {draft && (
          <Button
            disabled={
              busy ||
              configuration.blockingReasons.length > 0 ||
              !d.isCurrentVersion
            }
            onClick={() => setConfirm('send')}
          >
            {t.send}
          </Button>
        )}
        {d.canRevise && (
          <Button
            variant="outline"
            disabled={busy}
            onClick={() => setConfirm('revise')}
          >
            {t.revise}
          </Button>
        )}
        {d.isCurrentVersion && (draft || d.proposal.status === 'Sent') && (
          <Button
            variant="outline"
            disabled={busy}
            onClick={() => setConfirm('cancel')}
          >
            {t.cancel}
          </Button>
        )}
        {d.customerLinkActive && (
          <Button
            variant="outline"
            disabled={busy}
            onClick={() => setConfirm('revoke-access')}
          >
            {t.revoke}
          </Button>
        )}
      </div>
      {confirm && (
        <section className={styles.notice}>
          <p>
            {confirm === 'send'
              ? t.sendConfirm
              : confirm === 'revise'
                ? t.reviseConfirm
                : confirm === 'cancel'
                  ? t.cancelConfirm
                  : t.revokeConfirm}
          </p>
          <div className={styles.actions}>
            <Button disabled={busy} onClick={() => void action()}>
              {busy ? t.saving : t.confirm}
            </Button>
            <Button
              variant="outline"
              disabled={busy}
              onClick={() => setConfirm(undefined)}
            >
              {t.keep}
            </Button>
          </div>
        </section>
      )}
      {error !== undefined && (
        <ResearchError status={error} retry={() => onSaved('')} />
      )}
    </section>
  );
}
