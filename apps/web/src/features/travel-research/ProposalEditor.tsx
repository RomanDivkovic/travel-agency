import { useState, type FormEvent } from 'react';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { proposalApi } from './api';
import { useResearchCopy } from './copy';
import { AlternativeEditor } from './AlternativeEditor';
import { reorder } from './formHelpers';
import { ResearchError, TextArea, TextField } from './ResearchShared';
import type {
  AdminProposal,
  AlternativeInput,
  ProposalConfiguration,
  ProposalInput,
  ResearchOption,
} from './types';
import styles from './Research.module.scss';
export function ProposalEditor({
  detail: d,
  options,
  configuration,
  onSaved,
}: {
  detail: AdminProposal;
  options: ResearchOption[];
  configuration: ProposalConfiguration;
  onSaved: () => void;
}) {
  const t = useResearchCopy();
  const p = d.proposal;
  const [form, setForm] = useState<ProposalInput>({
    versionToken: d.versionToken,
    title: p.title,
    introduction: p.introduction,
    summary: p.summary,
    termsSnapshot: p.termsSnapshot,
    validUntil: p.validUntil,
    alternatives: p.alternatives.map((a, n) => ({
      title: a.title,
      description: a.description,
      recommended: a.recommended,
      itemIds: d.itemIds[n] ?? [],
    })),
  });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  function update<K extends keyof ProposalInput>(
    key: K,
    value: ProposalInput[K],
  ) {
    setForm((f) => ({ ...f, [key]: value }));
  }
  function choice(index: number, value: AlternativeInput) {
    update(
      'alternatives',
      form.alternatives.map((a, n) => (n === index ? value : a)),
    );
  }
  async function save(e: FormEvent) {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setError(undefined);
    try {
      await proposalApi.save(d.id, form);
      onSaved();
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className={styles.panel}>
      <h2>{t.edit}</h2>
      <form className={styles.form} onSubmit={(e) => void save(e)}>
        <fieldset disabled={busy}>
          <div className={styles.form}>
            <TextField
              label={t.title}
              value={form.title}
              onChange={(v) => update('title', v)}
            />
            <TextArea
              label={t.introduction}
              value={form.introduction}
              maxLength={3000}
              onChange={(v) => update('introduction', v)}
            />
            <TextArea
              label={t.summary}
              value={form.summary}
              maxLength={3000}
              onChange={(v) => update('summary', v)}
            />
            <TextField
              label={t.until}
              type="datetime-local"
              value={form.validUntil.slice(0, 16)}
              onChange={(v) => update('validUntil', v ? v + ':00Z' : '')}
            />
            {form.alternatives.map((a, n) => (
              <AlternativeEditor
                key={n}
                value={a}
                index={n}
                options={options}
                count={form.alternatives.length}
                onChange={(v) => choice(n, v)}
                onRecommend={(v) =>
                  update(
                    'alternatives',
                    form.alternatives.map((a, i) => ({
                      ...a,
                      recommended: i === n ? v : false,
                    })),
                  )
                }
                onRemove={() =>
                  update(
                    'alternatives',
                    form.alternatives.filter((_, i) => i !== n),
                  )
                }
                onMove={(dir) =>
                  update('alternatives', reorder(form.alternatives, n, dir))
                }
              />
            ))}
            <Button
              variant="outline"
              disabled={form.alternatives.length >= 6}
              onClick={() =>
                update('alternatives', [
                  ...form.alternatives,
                  {
                    title: '',
                    description: '',
                    recommended: false,
                    itemIds: [],
                  },
                ])
              }
            >
              {t.addAlternative}
            </Button>
            <TextArea
              label={t.terms}
              value={form.termsSnapshot}
              maxLength={6000}
              onChange={(v) => update('termsSnapshot', v)}
            />
            <Button
              variant="outline"
              onClick={() =>
                update(
                  'termsSnapshot',
                  p.language === 'sv'
                    ? configuration.termsSv
                    : configuration.termsEn,
                )
              }
            >
              {t.applyTerms}
            </Button>
            <Button type="submit" disabled={busy}>
              {busy ? t.saving : t.saveProposal}
            </Button>
          </div>
        </fieldset>
      </form>
      {error !== undefined && <ResearchError status={error} />}
    </section>
  );
}
