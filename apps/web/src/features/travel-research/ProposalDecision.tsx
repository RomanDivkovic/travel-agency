import { useRef, useState } from 'react';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { publicProposalApi } from './api';
import { useResearchCopy } from './copy';
import { ProposalCosts } from './ProposalSummary';
import { ResearchError, TextArea } from './ResearchShared';
import type { CustomerProposal } from './types';
import styles from './Research.module.scss';
export function ProposalDecision({
  proposal: p,
  token,
  onConfirmed,
  onRefresh,
}: {
  proposal: CustomerProposal;
  token: string;
  onConfirmed: (p: CustomerProposal) => void;
  onRefresh: () => void;
}) {
  const t = useResearchCopy();
  const [selected, setSelected] = useState('');
  const [ack, setAck] = useState(false);
  const [feedback, setFeedback] = useState('');
  const [confirm, setConfirm] = useState<'approve' | 'reject'>();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  const pending = useRef(false);
  const choice = p.alternatives.find((a) => a.id === selected);
  async function respond() {
    if (
      pending.current ||
      !confirm ||
      (confirm === 'approve' && (!choice || !ack))
    )
      return;
    pending.current = true;
    setBusy(true);
    setError(undefined);
    try {
      onConfirmed(
        await publicProposalApi.respond(
          token,
          p,
          confirm === 'approve',
          confirm === 'approve' ? selected : null,
          feedback,
        ),
      );
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      pending.current = false;
      setBusy(false);
    }
  }
  return (
    <section className={styles.panel}>
      <h2>{t.choose}</h2>
      <fieldset className={styles.choices} disabled={busy}>
        <legend>{t.alternatives}</legend>
        {p.alternatives.map((a) => (
          <label key={a.id} className={styles.check}>
            <input
              type="radio"
              name="proposal-alternative"
              value={a.id}
              checked={selected === a.id}
              onChange={() => {
                setSelected(a.id);
                setConfirm(undefined);
                setAck(false);
              }}
            />
            <span>{a.title}</span>
          </label>
        ))}
      </fieldset>
      {choice && (
        <section className={styles.notice}>
          <h3>
            {t.selection}: {choice.title}
          </h3>
          <ProposalCosts costs={choice.estimatedCosts} />
        </section>
      )}
      <label className={styles.check}>
        <input
          type="checkbox"
          checked={ack}
          disabled={busy}
          onChange={(e) => setAck(e.target.checked)}
        />
        <span>{p.acceptanceText}</span>
      </label>
      <div className={styles.form}>
        <TextArea
          label={t.feedback}
          value={feedback}
          maxLength={1000}
          onChange={setFeedback}
        />
      </div>
      <p>{t.priceNotice}</p>
      <p>{t.noBooking}</p>
      <div className={styles.actions}>
        <Button
          disabled={!choice || !ack || !p.acceptanceText || busy}
          onClick={() => setConfirm('approve')}
        >
          {t.approve}
        </Button>
        <Button
          variant="outline"
          disabled={busy}
          onClick={() => setConfirm('reject')}
        >
          {t.reject}
        </Button>
      </div>
      {confirm && (
        <section className={styles.notice}>
          <h3>{confirm === 'approve' ? t.approveConfirm : t.rejectConfirm}</h3>
          {confirm === 'approve' && choice && (
            <>
              <p>{choice.title}</p>
              <ProposalCosts costs={choice.estimatedCosts} />
              <p>{t.priceNotice}</p>
            </>
          )}
          <div className={styles.actions}>
            <Button
              disabled={busy || (confirm === 'approve' && !ack)}
              onClick={() => void respond()}
            >
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
        <ResearchError status={error} retry={onRefresh} />
      )}
    </section>
  );
}
