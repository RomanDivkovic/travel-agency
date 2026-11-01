import { useRef, useState, type FormEvent } from 'react';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { useLanguage } from '../../localization/useLanguage';
import { quotationApi } from './api';
import { useQuotationCopy } from './copy';
import {
  calculatePreview,
  formatMoney,
  localDateTime,
  minorInput,
  parseMinor,
} from './money';
import type {
  AdminQuotation,
  CustomerQuotation,
  DraftInput,
  QuotationConfiguration,
  TaxTreatment,
} from './types';
import { taxTreatments } from './types';
import styles from './Quotations.module.scss';
interface FormLine {
  key: string;
  description: string;
  quantity: string;
  unitPrice: string;
  taxRate: string;
}
export function QuotationEditor({
  detail,
  configuration,
  onSaved,
}: {
  detail: AdminQuotation;
  configuration: QuotationConfiguration;
  onSaved: () => void;
}) {
  const q = detail.quotation;
  const t = useQuotationCopy();
  const { language } = useLanguage();
  const [description, setDescription] = useState(q.description);
  const [included, setIncluded] = useState(q.includedServices);
  const [excluded, setExcluded] = useState(q.excludedServices);
  const [terms, setTerms] = useState(q.termsSnapshot);
  const [notes, setNotes] = useState(q.customerNotes);
  const [currency, setCurrency] = useState(q.currency);
  const [tax, setTax] = useState(q.taxTreatment);
  const [until, setUntil] = useState(localDateTime(q.validUntil));
  const [lines, setLines] = useState<FormLine[]>(() =>
    q.lines.map((l) => ({
      key: crypto.randomUUID(),
      description: l.description,
      quantity: String(l.quantity),
      unitPrice: minorInput(l.unitPriceMinorUnits),
      taxRate: minorInput(l.taxRateBasisPoints),
    })),
  );
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  const inFlight = useRef(false);
  const prepared = lines.map((l) => ({
    description: l.description,
    quantity: Number(l.quantity),
    unitPriceMinorUnits: parseMinor(l.unitPrice) ?? -1,
    taxRateBasisPoints: parseMinor(l.taxRate) ?? -1,
  }));
  const preview = calculatePreview(prepared, tax);
  function changeLine(
    key: string,
    field: keyof Omit<FormLine, 'key'>,
    value: string,
  ) {
    setLines((ls) =>
      ls.map((l) => (l.key === key ? { ...l, [field]: value } : l)),
    );
  }
  async function save(e: FormEvent) {
    e.preventDefault();
    if (inFlight.current) return;
    if (
      !preview ||
      lines.some((l) => !l.description.trim() || !/^\d+$/.test(l.quantity)) ||
      !until ||
      !Number.isFinite(new Date(until).getTime())
    ) {
      setError(400);
      return;
    }
    const input: DraftInput = {
      versionToken: detail.versionToken,
      description,
      includedServices: included,
      excludedServices: excluded,
      termsSnapshot: terms,
      customerNotes: notes,
      currency,
      taxTreatment: tax,
      validUntil: new Date(until).toISOString(),
      lines: prepared,
    };
    inFlight.current = true;
    setBusy(true);
    setError(undefined);
    try {
      await quotationApi.save(detail.id, input);
      onSaved();
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      inFlight.current = false;
      setBusy(false);
    }
  }
  return (
    <section className={styles.panel}>
      <h2>{t.edit}</h2>
      <p>{t.draftHint}</p>
      <form onSubmit={(e) => void save(e)} aria-busy={busy}>
        <fieldset disabled={busy}>
          <label>
            {t.description}
            <textarea
              aria-label={t.description}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              required
              maxLength={1000}
            />
          </label>
          <div className={styles.twoColumns}>
            <label>
              {t.included}
              <textarea
                aria-label={t.included}
                value={included}
                onChange={(e) => setIncluded(e.target.value)}
                maxLength={2000}
              />
            </label>
            <label>
              {t.excluded}
              <textarea
                aria-label={t.excluded}
                value={excluded}
                onChange={(e) => setExcluded(e.target.value)}
                maxLength={2000}
              />
            </label>
          </div>
          <div className={styles.twoColumns}>
            <label>
              {t.currency}
              <select
                aria-label={t.currency}
                value={currency}
                onChange={(e) =>
                  setCurrency(e.target.value as CustomerQuotation['currency'])
                }
              >
                {['SEK', 'EUR', 'USD'].map((c) => (
                  <option key={c}>{c}</option>
                ))}
              </select>
            </label>
            <label>
              {t.tax}
              <select
                aria-label={t.tax}
                value={tax}
                onChange={(e) => {
                  const mode = e.target.value as TaxTreatment;
                  setTax(mode);
                  if (mode === 'NoVat' || mode === 'Unconfirmed')
                    setLines((ls) =>
                      ls.map((l) => ({ ...l, taxRate: '0.00' })),
                    );
                }}
              >
                {taxTreatments.map((mode) => (
                  <option key={mode} value={mode}>
                    {t.taxModes[mode]}
                  </option>
                ))}
              </select>
            </label>
            <label>
              {t.validUntil}
              <input
                type="datetime-local"
                required
                aria-label={t.validUntil}
                value={until}
                onChange={(e) => setUntil(e.target.value)}
              />
              <span>
                {t.timeZone}: {Intl.DateTimeFormat().resolvedOptions().timeZone}
              </span>
            </label>
          </div>
          <h3>{t.items}</h3>
          <p>{t.limits}</p>
          {lines.map((line, i) => (
            <fieldset className={styles.lineEditor} key={line.key}>
              <legend>
                {t.item} {i + 1}
              </legend>
              <label>
                {t.lineDescription} {i + 1}
                <input
                  required
                  maxLength={200}
                  value={line.description}
                  onChange={(e) =>
                    changeLine(line.key, 'description', e.target.value)
                  }
                />
              </label>
              <label>
                {t.quantity} {i + 1}
                <input
                  type="number"
                  min={1}
                  max={100}
                  step={1}
                  required
                  value={line.quantity}
                  onChange={(e) =>
                    changeLine(line.key, 'quantity', e.target.value)
                  }
                />
              </label>
              <label>
                {t.unitPrice} {i + 1}
                <input
                  type="text"
                  inputMode="decimal"
                  pattern="[0-9]+([.][0-9]{1,2})?"
                  required
                  value={line.unitPrice}
                  onChange={(e) =>
                    changeLine(line.key, 'unitPrice', e.target.value)
                  }
                />
              </label>
              <label>
                {t.taxRate} {i + 1}
                <input
                  type="text"
                  inputMode="decimal"
                  pattern="[0-9]+([.][0-9]{1,2})?"
                  required
                  disabled={tax === 'NoVat' || tax === 'Unconfirmed'}
                  value={line.taxRate}
                  onChange={(e) =>
                    changeLine(line.key, 'taxRate', e.target.value)
                  }
                />
              </label>
              <Button
                variant="outline"
                disabled={lines.length === 1}
                onClick={() =>
                  setLines((ls) => ls.filter((l) => l.key !== line.key))
                }
                aria-label={`${t.removeLine} ${i + 1}`}
              >
                {t.removeLine}
              </Button>
            </fieldset>
          ))}
          <Button
            variant="outline"
            disabled={lines.length >= 20}
            onClick={() =>
              setLines((ls) => [
                ...ls,
                {
                  key: crypto.randomUUID(),
                  description: '',
                  quantity: '1',
                  unitPrice: '0.00',
                  taxRate: '0.00',
                },
              ])
            }
          >
            {t.addLine}
          </Button>
          {preview ? (
            <p role="status">
              {t.localPreview}: {formatMoney(preview.total, currency, language)}
            </p>
          ) : (
            <p>{t.invalid}</p>
          )}
          <label>
            {t.terms}
            <textarea
              maxLength={6000}
              aria-label={t.terms}
              value={terms}
              onChange={(e) => setTerms(e.target.value)}
            />
          </label>
          <p>{t.termsMismatch}</p>
          <Button
            variant="outline"
            onClick={() =>
              setTerms(
                q.language === 'sv'
                  ? configuration.termsSv
                  : configuration.termsEn,
              )
            }
          >
            {t.approvedTerms}
          </Button>
          <label>
            {t.notes}
            <textarea
              maxLength={2000}
              aria-label={t.notes}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </label>
        </fieldset>
        {error !== undefined ? (
          <p role="alert">{error === 409 ? t.conflict : t.invalid}</p>
        ) : null}
        <Button type="submit" disabled={busy}>
          {busy ? t.saving : t.save}
        </Button>
      </form>
    </section>
  );
}
