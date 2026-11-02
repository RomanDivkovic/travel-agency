import { emptyLeg } from './formHelpers';
import { useState, type FormEvent } from 'react';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { parseMinor } from '../quotations/money';
import { researchApi } from './api';
import { useResearchCopy } from './copy';
import { ExtraFields, FlightFields, StayFields } from './OptionDetailsFields';
import { ResearchError, TextArea, TextField } from './ResearchShared';
import type { ResearchInput, ResearchKind, ResearchOption } from './types';
import styles from './Research.module.scss';
function initial(kind: ResearchKind, option?: ResearchOption): ResearchInput {
  if (option) {
    const { totalMinorUnits, ...snapshot } = option.option;
    void totalMinorUnits;
    return { ...snapshot, notes: option.notes };
  }
  return {
    kind,
    title: '',
    description: '',
    priceMinorUnits: 0,
    currency: 'SEK',
    priceBasis: 'Total',
    quantity: 1,
    supplierName: '',
    supplierUrl: '',
    priceCheckedAt: new Date().toISOString(),
    availabilityCheckedAt: null,
    conditions: '',
    notes: '',
    flight:
      kind === 'Flight'
        ? { legs: [emptyLeg()], cabinClass: '', baggageDetails: '' }
        : null,
    accommodation:
      kind === 'Accommodation'
        ? {
            destination: '',
            address: '',
            accommodationType: '',
            roomType: '',
            checkIn: '',
            checkOut: '',
            guests: 1,
            mealPlan: '',
            amenities: '',
          }
        : null,
    extra:
      kind === 'Extra'
        ? { category: 'Transfer', location: '', date: null }
        : null,
  };
}
export function ResearchOptionEditor({
  requestId,
  kind,
  option,
  onSaved,
  onClose,
}: {
  requestId: string;
  kind: ResearchKind;
  option?: ResearchOption;
  onSaved: () => void;
  onClose: () => void;
}) {
  const t = useResearchCopy();
  const [form, setForm] = useState(() => initial(kind, option));
  const [price, setPrice] = useState(
    option ? (option.option.priceMinorUnits / 100).toFixed(2) : '',
  );
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  function update<K extends keyof ResearchInput>(
    key: K,
    value: ResearchInput[K],
  ) {
    setForm((v) => ({ ...v, [key]: value }));
  }
  async function save(event: FormEvent) {
    event.preventDefault();
    if (busy) return;
    const minor = parseMinor(price);
    if (typeof minor !== 'number') {
      setError(400);
      return;
    }
    setBusy(true);
    setError(undefined);
    try {
      await researchApi.saveOption(
        requestId,
        { ...form, priceMinorUnits: minor },
        option?.id,
        option?.versionToken,
      );
      onSaved();
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className={styles.panel}>
      <h2>{t.kinds[kind]}</h2>
      <p>{t.sourceNotice}</p>
      <form className={styles.form} onSubmit={(e) => void save(e)}>
        <fieldset disabled={busy}>
          <div className={styles.form}>
            <TextField
              label={t.title}
              value={form.title}
              onChange={(v) => update('title', v)}
            />
            <TextArea
              label={t.description}
              value={form.description}
              maxLength={2000}
              onChange={(v) => update('description', v)}
            />
            {form.flight && (
              <FlightFields
                value={form.flight}
                onChange={(v) => update('flight', v)}
              />
            )}
            {form.accommodation && (
              <StayFields
                value={form.accommodation}
                onChange={(v) => update('accommodation', v)}
              />
            )}
            {form.extra && (
              <ExtraFields
                value={form.extra}
                onChange={(v) => update('extra', v)}
              />
            )}
            <div className={styles.grid}>
              <TextField
                label={t.price}
                value={price}
                onChange={setPrice}
                maxLength={14}
              />
              <label>
                {t.currency}
                <select
                  value={form.currency}
                  onChange={(e) => update('currency', e.target.value)}
                >
                  {['SEK', 'EUR', 'USD'].map((c) => (
                    <option key={c}>{c}</option>
                  ))}
                </select>
              </label>
              <label>
                {t.basis}
                <select
                  value={form.priceBasis}
                  onChange={(e) => {
                    update('priceBasis', e.target.value);
                    if (e.target.value === 'Total') update('quantity', 1);
                  }}
                >
                  {Object.entries(t.bases).map(([key, label]) => (
                    <option key={key} value={key}>
                      {label}
                    </option>
                  ))}
                </select>
              </label>
              <TextField
                label={t.quantity}
                type="number"
                min={1}
                max={form.priceBasis === 'Total' ? 1 : 1000}
                value={form.quantity}
                onChange={(v) => update('quantity', Number(v))}
              />
            </div>
            <p>{t.quantityHint}</p>
            <TextField
              label={t.supplier}
              value={form.supplierName}
              onChange={(v) => update('supplierName', v)}
            />
            <TextField
              label={t.url}
              type="url"
              value={form.supplierUrl}
              maxLength={2000}
              onChange={(v) => update('supplierUrl', v)}
            />
            <TextField
              label={t.priceChecked}
              type="datetime-local"
              value={form.priceCheckedAt.slice(0, 16)}
              onChange={(v) => update('priceCheckedAt', v ? v + ':00Z' : '')}
            />
            <TextField
              label={t.availabilityChecked}
              type="datetime-local"
              required={false}
              value={form.availabilityCheckedAt?.slice(0, 16) ?? ''}
              onChange={(v) =>
                update('availabilityCheckedAt', v ? v + ':00Z' : null)
              }
            />
            <TextArea
              label={t.conditions}
              required
              value={form.conditions}
              maxLength={3000}
              onChange={(v) => update('conditions', v)}
            />
            <TextArea
              label={t.optionNotes}
              value={form.notes}
              maxLength={4000}
              onChange={(v) => update('notes', v)}
            />
            <p>{t.private}</p>
            <Button type="submit" disabled={busy}>
              {busy ? t.saving : t.save}
            </Button>
          </div>
        </fieldset>
      </form>
      {error !== undefined && <ResearchError status={error} />}
      <Button variant="text" disabled={busy} onClick={onClose}>
        {t.close}
      </Button>
    </section>
  );
}
