import { reorder } from './formHelpers';
import { Button } from '../../components/ui/Button';
import { useResearchCopy } from './copy';
import { TextArea, TextField } from './ResearchShared';
import type { AlternativeInput, ResearchOption } from './types';
import styles from './Research.module.scss';
export function AlternativeEditor({
  value: a,
  index,
  options,
  onChange,
  onRecommend,
  onRemove,
  onMove,
  count,
}: {
  value: AlternativeInput;
  index: number;
  options: ResearchOption[];
  onChange: (v: AlternativeInput) => void;
  onRecommend: (v: boolean) => void;
  onRemove: () => void;
  onMove: (n: number) => void;
  count: number;
}) {
  const t = useResearchCopy();
  return (
    <fieldset>
      <legend>
        {t.alternative} {index + 1}
      </legend>
      <div className={styles.form}>
        <TextField
          label={`${t.alternative} ${index + 1}: ${t.title}`}
          value={a.title}
          onChange={(v) => onChange({ ...a, title: v })}
        />
        <TextArea
          label={`${t.alternative} ${index + 1}: ${t.description}`}
          value={a.description}
          maxLength={2000}
          required
          onChange={(v) => onChange({ ...a, description: v })}
        />
        <label className={styles.check}>
          <input
            type="checkbox"
            checked={a.recommended}
            onChange={(e) => onRecommend(e.target.checked)}
          />
          <span>{t.recommend}</span>
        </label>
        <fieldset>
          <legend>{t.chooseItems}</legend>
          {options.map((o) => (
            <label key={o.id} className={styles.check}>
              <input
                type="checkbox"
                checked={a.itemIds.includes(o.id)}
                disabled={!a.itemIds.includes(o.id) && a.itemIds.length >= 20}
                onChange={(e) =>
                  onChange({
                    ...a,
                    itemIds: e.target.checked
                      ? [...a.itemIds, o.id]
                      : a.itemIds.filter((id) => id !== o.id),
                  })
                }
              />
              <span>
                {o.option.title} · {t.kinds[o.option.kind]} ·{' '}
                {o.option.currency}
              </span>
            </label>
          ))}
        </fieldset>
        <ol>
          {a.itemIds.map((id, n) => (
            <li key={id}>
              {options.find((o) => o.id === id)?.option.title ?? id}
              <div className={styles.actions}>
                <Button
                  variant="text"
                  disabled={n === 0}
                  onClick={() =>
                    onChange({ ...a, itemIds: reorder(a.itemIds, n, -1) })
                  }
                >
                  {t.up}
                </Button>
                <Button
                  variant="text"
                  disabled={n === a.itemIds.length - 1}
                  onClick={() =>
                    onChange({ ...a, itemIds: reorder(a.itemIds, n, 1) })
                  }
                >
                  {t.down}
                </Button>
              </div>
            </li>
          ))}
        </ol>
        <div className={styles.actions}>
          <Button
            variant="outline"
            disabled={index === 0}
            onClick={() => onMove(-1)}
          >
            {t.up} {t.alternative}
          </Button>
          <Button
            variant="outline"
            disabled={index === count - 1}
            onClick={() => onMove(1)}
          >
            {t.down} {t.alternative}
          </Button>
          <Button variant="text" onClick={onRemove}>
            {t.remove} {t.alternative}
          </Button>
        </div>
      </div>
    </fieldset>
  );
}
