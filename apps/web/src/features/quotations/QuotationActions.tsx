import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { quotationApi } from './api';
import { useQuotationCopy } from './copy';
import { QuotationError } from './QuotationError';
import type { AdminQuotation, QuotationConfiguration } from './types';
import styles from './Quotations.module.scss';
export function QuotationActions({
  detail,
  configuration,
  onSaved,
}: {
  detail: AdminQuotation;
  configuration: QuotationConfiguration;
  onSaved: (kind: string) => void;
}) {
  const t = useQuotationCopy();
  const navigate = useNavigate();
  const [action, setAction] = useState<'send' | 'revise' | 'cancel'>();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  const title = useRef<HTMLHeadingElement>(null);
  const trigger = useRef<HTMLButtonElement | null>(null);
  const pending = useRef(false);
  useEffect(() => {
    if (action) title.current?.focus();
  }, [action]);
  async function run() {
    if (!action || pending.current) return;
    pending.current = true;
    setBusy(true);
    setError(undefined);
    try {
      if (action === 'revise') {
        const result = await quotationApi.revise(
          detail.id,
          detail.versionToken,
        );
        await navigate('/admin/quotations/' + result.id);
      } else {
        await quotationApi[action](detail.id, detail.versionToken);
        onSaved(action);
      }
      setAction(undefined);
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      pending.current = false;
      setBusy(false);
    }
  }
  const current = detail.isCurrentVersion;
  const status = detail.quotation.status;
  const canSend =
    current &&
    status === 'Draft' &&
    configuration.issuanceReady &&
    detail.quotation.taxTreatment !== 'Unconfirmed' &&
    detail.quotation.totalMinorUnits > 0;
  return (
    <section className={styles.panel}>
      {!configuration.issuanceReady ? <p>{t.blocked}</p> : null}
      {!current ? <p>{t.historical}</p> : null}
      {status !== 'Draft' ? (
        <p>{status === 'Accepted' ? t.acceptedLocked : t.immutable}</p>
      ) : null}
      <div className={styles.actions}>
        {current && status === 'Draft' ? (
          <Button
            disabled={!canSend || busy}
            onClick={(e) => {
              trigger.current = e.currentTarget;
              setAction('send');
            }}
          >
            {t.send}
          </Button>
        ) : null}
        {detail.canRevise ? (
          <Button
            variant="outline"
            disabled={busy}
            onClick={(e) => {
              trigger.current = e.currentTarget;
              setAction('revise');
            }}
          >
            {t.revise}
          </Button>
        ) : null}
        {current && (status === 'Draft' || status === 'Sent') ? (
          <Button
            variant="outline"
            disabled={busy}
            onClick={(e) => {
              trigger.current = e.currentTarget;
              setAction('cancel');
            }}
          >
            {t.cancel}
          </Button>
        ) : null}
      </div>
      {action ? (
        <section className={styles.confirm}>
          <h2 ref={title} tabIndex={-1}>
            {action === 'send'
              ? t.confirmSend
              : action === 'revise'
                ? t.confirmRevise
                : t.confirmCancel}
          </h2>
          {action === 'revise' ? <p>{t.immutable}</p> : null}
          <Button disabled={busy} onClick={() => void run()}>
            {busy ? t.saving : t.confirm}
          </Button>
          <Button
            variant="text"
            disabled={busy}
            onClick={() => {
              setAction(undefined);
              trigger.current?.focus();
            }}
          >
            {t.keep}
          </Button>
        </section>
      ) : null}
      {error !== undefined ? (
        <QuotationError status={error} retry={() => onSaved('refresh')} />
      ) : null}
    </section>
  );
}
