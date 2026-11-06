import { MediaSelector } from './MediaSelector';
import { useState } from 'react';
import { useCmsCopy, fieldLabel } from './copy';
import { TextField, Check, Select } from './Fields';
import { useLanguage } from '../../localization/useLanguage';
import type {
  ServiceData,
  FaqData,
  PolicyData,
  PageData,
  Language,
  MediaItem,
} from './types';
import styles from './Management.module.scss';
export function LanguageTabs({
  language,
  onChange,
}: {
  language: Language;
  onChange: (l: Language) => void;
}) {
  const t = useCmsCopy();
  return (
    <Select
      label={t.language}
      value={language}
      onChange={(v) => onChange(v as Language)}
    >
      <option value="sv">Svenska</option>
      <option value="en">English</option>
    </Select>
  );
}
export function ServiceEditor({
  data,
  onChange,
}: {
  data: ServiceData;
  onChange: (d: ServiceData) => void;
}) {
  const t = useCmsCopy();
  const { language } = useLanguage();
  const [edit, setEdit] = useState<Language>(language);
  const p = data.pricing;
  return (
    <>
      <p>{t.note}</p>
      <p>{data.code}</p>
      <Check
        label={t.enabled}
        checked={data.enabled}
        onChange={(v) => onChange({ ...data, enabled: v })}
      />
      <TextField
        label={t.order}
        type="number"
        value={String(data.displayOrder)}
        required
        onChange={(v) => onChange({ ...data, displayOrder: Number(v) })}
      />
      <LanguageTabs language={edit} onChange={setEdit} />
      {(Object.keys(data[edit]) as (keyof ServiceData['sv'])[]).map((k) => (
        <TextField
          key={k}
          label={fieldLabel(k, language === 'sv')}
          value={data[edit][k]}
          required
          type={k === 'name' || k === 'cta' ? 'text' : 'textarea'}
          maxLength={k === 'name' ? 150 : k === 'cta' ? 100 : 5000}
          onChange={(v) =>
            onChange({ ...data, [edit]: { ...data[edit], [k]: v } })
          }
        />
      ))}
      <h2>{t.services}</h2>
      <Select
        label={t.priceType}
        value={p.type}
        onChange={(v) =>
          onChange({
            ...data,
            pricing: {
              ...p,
              type: v as typeof p.type,
              amountMinor: v === 'CustomQuotation' ? null : 0,
            },
          })
        }
      >
        <option value="CustomQuotation">{t.custom}</option>
        <option value="FixedFee">{t.fixed}</option>
        <option value="StartingFrom">{t.starting}</option>
      </Select>
      {p.type !== 'CustomQuotation' && (
        <MoneyField
          value={p.amountMinor ?? 0}
          onChange={(v) =>
            onChange({ ...data, pricing: { ...p, amountMinor: v } })
          }
        />
      )}
      <Select
        label={t.currency}
        value={p.currency}
        onChange={(v) => onChange({ ...data, pricing: { ...p, currency: v } })}
      >
        {['SEK', 'EUR', 'USD', 'GBP', 'NOK', 'DKK'].map((c) => (
          <option key={c}>{c}</option>
        ))}
      </Select>
      {(['effectiveFrom', 'effectiveUntil'] as const).map((k) => (
        <TextField
          key={k}
          label={k === 'effectiveFrom' ? t.from : t.until}
          type="datetime-local"
          value={p[k]?.slice(0, 16) || ''}
          onChange={(v) =>
            onChange({
              ...data,
              pricing: {
                ...p,
                [k]: v ? new Date(v + 'Z').toISOString() : null,
              },
            })
          }
        />
      ))}
      <p>UTC</p>
    </>
  );
}
function MoneyField({
  value,
  onChange,
}: {
  value: number;
  onChange: (n: number) => void;
}) {
  const t = useCmsCopy();
  const [text, setText] = useState((value / 100).toFixed(2));
  return (
    <label className={styles.field}>
      <span>{t.amount}</span>
      <input
        inputMode="decimal"
        required
        pattern="[0-9]+([.,][0-9]{1,2})?"
        value={text}
        onChange={(e) => {
          const v = e.target.value;
          setText(v);
          if (/^\d+([.,]\d{1,2})?$/.test(v)) {
            const [whole, fraction = ''] = v.replace(',', '.').split('.');
            onChange(Number(whole) * 100 + Number(fraction.padEnd(2, '0')));
          }
        }}
      />
    </label>
  );
}
export function FaqEditor({
  data,
  onChange,
}: {
  data: FaqData;
  onChange: (d: FaqData) => void;
}) {
  const t = useCmsCopy();
  const { language } = useLanguage();
  const [edit, setEdit] = useState<Language>(language);
  return (
    <>
      <Check
        label={t.enabled}
        checked={data.enabled}
        onChange={(v) => onChange({ ...data, enabled: v })}
      />
      <TextField
        label={t.order}
        type="number"
        value={String(data.displayOrder)}
        required
        onChange={(v) => onChange({ ...data, displayOrder: Number(v) })}
      />
      <LanguageTabs language={edit} onChange={setEdit} />
      {(['question', 'answer'] as const).map((k) => (
        <TextField
          key={k}
          label={fieldLabel(k, language === 'sv')}
          type={k === 'answer' ? 'textarea' : 'text'}
          value={data[edit][k]}
          required
          maxLength={k === 'question' ? 250 : 5000}
          onChange={(v) =>
            onChange({ ...data, [edit]: { ...data[edit], [k]: v } })
          }
        />
      ))}
    </>
  );
}
export function PolicyEditor({
  data,
  onChange,
}: {
  data: PolicyData;
  onChange: (d: PolicyData) => void;
}) {
  const t = useCmsCopy();
  const { language } = useLanguage();
  const [edit, setEdit] = useState<Language>(language);
  return (
    <>
      <p className={styles.notice}>{t.legal}</p>
      <LanguageTabs language={edit} onChange={setEdit} />
      {(['title', 'content'] as const).map((k) => (
        <TextField
          key={k}
          label={fieldLabel(k, language === 'sv')}
          type={k === 'content' ? 'textarea' : 'text'}
          value={data[edit][k]}
          required
          maxLength={k === 'content' ? 30000 : 200}
          onChange={(v) =>
            onChange({
              ...data,
              [edit]: { ...data[edit], [k]: v },
              legallyReviewed: false,
            })
          }
        />
      ))}
      <TextField
        label={t.from}
        type="date"
        required
        value={data.effectiveFrom}
        onChange={(v) =>
          onChange({ ...data, effectiveFrom: v, legallyReviewed: false })
        }
      />
      <TextField
        label={t.reviewedBy}
        value={data.reviewedBy}
        onChange={(v) => onChange({ ...data, reviewedBy: v })}
      />
      <TextField
        label={t.reviewedOn}
        type="date"
        value={data.reviewedOn || ''}
        onChange={(v) => onChange({ ...data, reviewedOn: v || null })}
      />
      <Check
        label={t.reviewed}
        checked={data.legallyReviewed}
        onChange={(v) => onChange({ ...data, legallyReviewed: v })}
      />
    </>
  );
}
export function PageEditor({
  data,
  onChange,
  media,
}: {
  data: PageData;
  onChange: (d: PageData) => void;
  media: MediaItem[];
}) {
  const t = useCmsCopy();
  const { language } = useLanguage();
  const [edit, setEdit] = useState<Language>(language);
  const groups = new Map<string, string[]>();
  for (const k of Object.keys(data[edit])) {
    const group = k.split('.').slice(0, 2).join('.');
    groups.set(group, [...(groups.get(group) || []), k]);
  }
  return (
    <>
      <LanguageTabs language={edit} onChange={setEdit} />
      {[...groups].map(([section, keys]) => (
        <details
          className={styles.panel}
          open={groups.size === 1}
          key={section}
        >
          <summary>{fieldLabel(section, language === 'sv')}</summary>
          {keys.map((k) => (
            <TextField
              key={k}
              label={fieldLabel(k, language === 'sv')}
              value={data[edit][k] ?? ''}
              required
              type="textarea"
              onChange={(v) =>
                onChange({ ...data, [edit]: { ...data[edit], [k]: v } })
              }
            />
          ))}
        </details>
      ))}
      <MediaSelector
        image={data.image}
        initial={media}
        onChange={(image) => onChange({ ...data, image })}
      />
      {data.image && (
        <>
          <TextField
            label={t.altSv}
            value={data.image.altSv}
            required={!data.image.decorative}
            onChange={(v) =>
              onChange({ ...data, image: { ...data.image!, altSv: v } })
            }
          />
          <TextField
            label={t.altEn}
            value={data.image.altEn}
            required={!data.image.decorative}
            onChange={(v) =>
              onChange({ ...data, image: { ...data.image!, altEn: v } })
            }
          />
          <Check
            label={t.decorative}
            checked={data.image.decorative}
            onChange={(v) =>
              onChange({
                ...data,
                image: {
                  ...data.image!,
                  decorative: v,
                  altSv: v ? '' : data.image!.altSv,
                  altEn: v ? '' : data.image!.altEn,
                },
              })
            }
          />
        </>
      )}
    </>
  );
}
