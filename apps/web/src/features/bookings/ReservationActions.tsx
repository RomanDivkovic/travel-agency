import { utcInput } from './time';
import { useState, type FormEvent } from 'react';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { parseMinor } from '../quotations/money';
import { TextArea, TextField } from '../travel-research/ResearchShared';
import { bookingApi } from './api';
import { useBookingCopy } from './copy';
import { BookingError, ConfirmAction, EvidenceForm } from './BookingShared';
import { ReservationEditor } from './ReservationEditor';
import type { Booking, Reservation } from './types';
import styles from './Booking.module.scss';
export function ReservationActions({
  detail,
  r,
  onSaved,
}: {
  detail: Booking;
  r: Reservation;
  onSaved: () => void;
}) {
  const t = useBookingCopy();
  const [mode, setMode] = useState('');
  const [error, setError] = useState<number>();
  const [busy, setBusy] = useState(false);
  const [reference, setReference] = useState('');
  const [evidence, setEvidence] = useState('');
  const [document, setDocument] = useState('');
  const [at, setAt] = useState('');
  const [ticketing, setTicketing] = useState(r.ticketingStatus);
  const [charge, setCharge] = useState('');
  const [explicit, setExplicit] = useState(false);
  const mutate = async (action: string, input: object = {}) => {
    await bookingApi.reservation(
      detail.booking.id,
      r.id,
      detail.versionToken,
      action,
      input,
    );
    onSaved();
  };
  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!explicit) return;
    setBusy(true);
    setError(undefined);
    try {
      if (mode === 'confirm')
        await mutate('confirm', {
          supplierReference: reference,
          evidence,
          documentReference: document,
          ticketingStatus: ticketing,
          confirmedAt: utcInput(at),
        });
      if (mode === 'fail') await mutate('fail', { reason: evidence });
      if (mode === 'cancel') {
        const minor = charge ? parseMinor(charge) : null;
        if (charge && minor === undefined) {
          setError(400);
          return;
        }
        await mutate('cancel', {
          reason: evidence,
          supplierEvidence: reference,
          chargeMinorUnits: minor,
          cancelledAt: utcInput(at),
        });
      }
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  const choose = (m: string) => {
    setMode(m);
    setExplicit(false);
    setError(undefined);
  };
  return (
    <>
      <div className={styles.actions}>
        {detail.eligible && r.status !== 'Cancelled' && (
          <Button variant="outline" onClick={() => choose('edit')}>
            {t.edit}
          </Button>
        )}
        {detail.eligible && r.requiresReview && !r.reapprovalRecorded && (
          <Button onClick={() => choose('reapprove')}>{t.reapprove}</Button>
        )}
        {detail.eligible && ['NotStarted', 'Failed'].includes(r.status) && (
          <ConfirmAction
            label={t.start}
            hint={t.authorityHint}
            onConfirm={() => mutate('start')}
          />
        )}
        {detail.eligible && r.status === 'InProgress' && (
          <ConfirmAction
            label={t.await}
            hint={t.confirmHint}
            onConfirm={() => mutate('await')}
          />
        )}
        {detail.eligible &&
          ['InProgress', 'AwaitingConfirmation', 'Confirmed'].includes(
            r.status,
          ) && <Button onClick={() => choose('confirm')}>{t.confirm}</Button>}
        {['InProgress', 'AwaitingConfirmation'].includes(r.status) && (
          <Button variant="outline" onClick={() => choose('fail')}>
            {t.fail}
          </Button>
        )}
        {r.status !== 'Cancelled' && (
          <Button variant="outline" onClick={() => choose('cancel')}>
            {t.cancellation}
          </Button>
        )}
      </div>
      {mode === 'edit' ? (
        <ReservationEditor
          detail={detail}
          r={r}
          onSaved={onSaved}
          onClose={() => setMode('')}
        />
      ) : mode === 'reapprove' ? (
        <EvidenceForm
          onClose={() => setMode('')}
          onSave={(e) => mutate('reapprove', { evidence: e })}
        />
      ) : ['confirm', 'cancel', 'fail'].includes(mode) ? (
        <form className={styles.form} onSubmit={(e) => void submit(e)}>
          <fieldset disabled={busy}>
            <legend>
              {mode === 'confirm'
                ? t.confirm
                : mode === 'cancel'
                  ? t.cancellation
                  : t.fail}
            </legend>
            <p>{mode === 'cancel' ? t.cancellationHint : t.confirmHint}</p>
            {mode !== 'fail' && (
              <TextField
                required={mode === 'cancel'}
                label={
                  mode === 'cancel' ? t.cancelEvidence : t.supplierReference
                }
                value={reference}
                onChange={setReference}
                maxLength={mode === 'cancel' ? 1000 : 200}
              />
            )}
            <TextArea
              required
              label={mode === 'confirm' ? t.supplierEvidence : t.reason}
              value={evidence}
              onChange={setEvidence}
              maxLength={2000}
            />
            {mode !== 'fail' && (
              <TextField
                type="datetime-local"
                step="0.001"
                label={mode === 'confirm' ? t.confirmedAt : t.cancelledAt}
                value={at}
                onChange={setAt}
              />
            )}
            {mode === 'cancel' && (
              <TextField
                required={false}
                label={t.charge}
                value={charge}
                onChange={setCharge}
                maxLength={14}
              />
            )}
            {mode === 'confirm' && (
              <>
                <TextField
                  required={false}
                  label={t.document}
                  value={document}
                  onChange={setDocument}
                  maxLength={1000}
                />
                {r.current.kind === 'Flight' && (
                  <label>
                    {t.ticketing}
                    <select
                      aria-label={t.ticketing}
                      value={ticketing}
                      onChange={(e) => setTicketing(e.target.value)}
                    >
                      <option value="NotTicketed">{t.notTicketed}</option>
                      <option value="Issued">{t.issued}</option>
                    </select>
                  </label>
                )}
              </>
            )}
            <label className={styles.check}>
              <input
                type="checkbox"
                required
                checked={explicit}
                onChange={(e) => setExplicit(e.target.checked)}
              />
              <span>{t.confirmAction}</span>
            </label>
            <Button type="submit" disabled={busy || !explicit}>
              {busy ? t.saving : t.save}
            </Button>
          </fieldset>
          {error !== undefined && <BookingError status={error} />}
          <Button variant="text" disabled={busy} onClick={() => setMode('')}>
            {t.cancel}
          </Button>
        </form>
      ) : null}
    </>
  );
}
