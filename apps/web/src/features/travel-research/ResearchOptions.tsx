import { useState } from 'react';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { researchApi } from './api';
import { useResearchCopy } from './copy';
import { ResearchOptionEditor } from './ResearchOptionEditor';
import { ResearchOptionSummary } from './ResearchOptionSummary';
import { ResearchError } from './ResearchShared';
import type { ResearchKind, ResearchOption } from './types';
import styles from './Research.module.scss';
export function ResearchOptions({
  requestId,
  options,
  eligible,
  onSaved,
  onRefresh,
}: {
  requestId: string;
  options: ResearchOption[];
  eligible: boolean;
  onSaved: () => void;
  onRefresh?: () => void;
}) {
  const t = useResearchCopy();
  const [editor, setEditor] = useState<{
    kind: ResearchKind;
    option?: ResearchOption;
  }>();
  const [selected, setSelected] = useState<string[]>([]);
  const [compare, setCompare] = useState(false);
  const [remove, setRemove] = useState<ResearchOption>();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  const comparison = options.filter((o) => selected.includes(o.id));
  function toggle(o: ResearchOption) {
    setCompare(false);
    setSelected((v) =>
      v.includes(o.id)
        ? v.filter((id) => id !== o.id)
        : v.length < 4 &&
            (!comparison.length || comparison[0]!.option.kind === o.option.kind)
          ? [...v, o.id]
          : v,
    );
  }
  async function removeOption() {
    if (!remove || busy) return;
    setBusy(true);
    setError(undefined);
    try {
      await researchApi.removeOption(requestId, remove.id, remove.versionToken);
      setRemove(undefined);
      setSelected([]);
      onSaved();
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <section>
      <h2>{t.options}</h2>
      <p>{t.sourceNotice}</p>
      {eligible && (
        <div className={styles.actions}>
          {(
            [
              ['Flight', t.addFlight],
              ['Accommodation', t.addStay],
              ['Extra', t.addExtra],
            ] as const
          ).map(([kind, label]) => (
            <Button
              key={kind}
              variant="outline"
              onClick={() => setEditor({ kind })}
            >
              {label}
            </Button>
          ))}
        </div>
      )}
      {editor && (
        <ResearchOptionEditor
          key={editor.option?.versionToken ?? editor.kind}
          requestId={requestId}
          {...editor}
          onClose={() => setEditor(undefined)}
          onSaved={() => {
            setEditor(undefined);
            onSaved();
          }}
        />
      )}
      <p>{t.compareHint}</p>
      <Button
        variant="outline"
        disabled={comparison.length < 2}
        onClick={() => setCompare((v) => !v)}
        aria-expanded={compare}
      >
        {t.compare}
      </Button>
      {compare && (
        <section className={styles.panel}>
          <h2>{t.comparison}</h2>
          <div className={styles.grid}>
            {comparison.map((o) => (
              <ResearchOptionSummary key={o.id} option={o.option} />
            ))}
          </div>
        </section>
      )}
      {!options.length && <p>{t.noOptions}</p>}
      <div className={styles.grid}>
        {options.map((o) => (
          <section key={o.id}>
            <ResearchOptionSummary option={o.option} />
            <label className={styles.check}>
              <input
                type="checkbox"
                checked={selected.includes(o.id)}
                aria-label={`${t.selectedCompare}: ${o.option.title}`}
                onChange={() => toggle(o)}
                disabled={
                  !selected.includes(o.id) &&
                  (selected.length >= 4 ||
                    (comparison.length > 0 &&
                      comparison[0]!.option.kind !== o.option.kind))
                }
              />
              <span>{t.selectedCompare}</span>
            </label>
            {o.notes && (
              <p className={styles.prose}>
                {t.notes}: {o.notes}
              </p>
            )}
            {eligible && (
              <div className={styles.actions}>
                <Button
                  variant="outline"
                  onClick={() => setEditor({ kind: o.option.kind, option: o })}
                >
                  {t.edit}: {o.option.title}
                </Button>
                <Button variant="text" onClick={() => setRemove(o)}>
                  {t.remove}: {o.option.title}
                </Button>
              </div>
            )}
          </section>
        ))}
      </div>
      {remove && (
        <section className={styles.notice}>
          <p>
            {t.removeConfirm} {remove.option.title}
          </p>
          <div className={styles.actions}>
            <Button disabled={busy} onClick={() => void removeOption()}>
              {t.confirm}
            </Button>
            <Button
              variant="outline"
              disabled={busy}
              onClick={() => setRemove(undefined)}
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
