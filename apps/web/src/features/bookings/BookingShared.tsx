import { utcInput } from './time';
import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { TextArea, TextField } from '../travel-research/ResearchShared';
import { useBookingCopy } from './copy';
import type { Evidence } from './types';
import styles from './Booking.module.scss';
export function BookingError({
  status,
  retry,
}: {
  status: number;
  retry?: () => void;
}) {
  const t = useBookingCopy();
  return (
    <div role="alert">
      <p>
        {status === 400
          ? t.invalid
          : status === 403
            ? t.blocked
            : status === 409
              ? t.conflict
              : t.error}
      </p>
      {retry && (
        <Button variant="outline" onClick={retry}>
          {t.retry}
        </Button>
      )}
    </div>
  );
}
export function EvidenceForm({
  onSave,
  onClose,
}: {
  onSave: (e: Evidence) => Promise<void>;
  onClose: () => void;
}) {
  const t = useBookingCopy();
  const [form, setForm] = useState<Evidence>({
    customerName: '',
    channel: 'Email',
    reference: '',
    approvedAt: '',
    confirmed: false,
  });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  async function save(e: FormEvent) {
    e.preventDefault();
    if (!form.confirmed) return;
    setBusy(true);
    setError(undefined);
    try {
      await onSave({ ...form, approvedAt: utcInput(form.approvedAt) });
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <form className={styles.form} onSubmit={(e) => void save(e)}>
      <fieldset disabled={busy}>
        <legend>{t.authorization}</legend>
        <p>{t.authorityHint}</p>
        <TextField
          label={t.customer}
          value={form.customerName}
          maxLength={100}
          onChange={(v) => setForm({ ...form, customerName: v })}
        />
        <label>
          {t.channel}
          <select
            aria-label={t.channel}
            value={form.channel}
            onChange={(e) => setForm({ ...form, channel: e.target.value })}
          >
            {Object.entries(t.channels).map(([v, label]) => (
              <option key={v} value={v}>
                {label}
              </option>
            ))}
          </select>
        </label>
        <TextArea
          required
          label={t.evidence}
          value={form.reference}
          maxLength={1000}
          onChange={(v) => setForm({ ...form, reference: v })}
        />
        <TextField
          type="datetime-local"
          step="0.001"
          label={t.approvedAt}
          value={form.approvedAt}
          onChange={(v) => setForm({ ...form, approvedAt: v })}
        />
        <label className={styles.check}>
          <input
            type="checkbox"
            required
            checked={form.confirmed}
            onChange={(e) => setForm({ ...form, confirmed: e.target.checked })}
          />
          <span>{t.explicit}</span>
        </label>
        <Button type="submit" disabled={busy || !form.confirmed}>
          {busy ? t.saving : t.save}
        </Button>
      </fieldset>
      {error !== undefined && <BookingError status={error} />}
      <Button variant="text" disabled={busy} onClick={onClose}>
        {t.cancel}
      </Button>
    </form>
  );
}
export function ConfirmAction({
  label,
  hint,
  onConfirm,
}: {
  label: string;
  hint: string;
  onConfirm: () => Promise<void>;
}) {
  const t = useBookingCopy();
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  const opener = useRef<HTMLButtonElement>(null);
  const confirmation = useRef<HTMLButtonElement>(null);
  const shouldRestore = useRef(false);
  useEffect(() => {
    if (open) {
      confirmation.current?.focus();
      shouldRestore.current = true;
    } else if (shouldRestore.current) {
      opener.current?.focus();
      shouldRestore.current = false;
    }
  }, [open]);
  async function run() {
    setBusy(true);
    try {
      await onConfirm();
      setOpen(false);
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <div>
      {!open ? (
        <Button
          ref={opener}
          variant="outline"
          onClick={() => {
            setError(undefined);
            setOpen(true);
          }}
        >
          {label}
        </Button>
      ) : (
        <section className={styles.notice} aria-label={label}>
          <p>{hint}</p>
          <div className={styles.actions}>
            <Button
              ref={confirmation}
              disabled={busy}
              onClick={() => void run()}
            >
              {busy ? t.saving : t.confirmAction}
            </Button>
            <Button
              disabled={busy}
              variant="text"
              onClick={() => setOpen(false)}
            >
              {t.cancel}
            </Button>
          </div>
        </section>
      )}
      {error !== undefined && <BookingError status={error} />}
    </div>
  );
}
