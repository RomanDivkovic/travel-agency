import { useCallback, useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { Button } from '../../components/ui/Button';
import { useApiResource } from '../../hooks/useApiResource';
import { ApiError } from '../../services/apiClient';
import { invoiceApi } from './api';
import { usePaymentCopy } from './copy';
import { PaymentError } from './PaymentError';
import styles from './Payments.module.scss';
export function QuotationInvoice({ quotationId }: { quotationId: string }) {
  const t = usePaymentCopy();
  const navigate = useNavigate();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  const [revision, setRevision] = useState(0);
  const load = useCallback(
    (signal: AbortSignal) => invoiceApi.forQuotation(quotationId, signal),
    [quotationId],
  );
  const state = useApiResource(String(revision), load);
  async function create() {
    if (busy) return;
    setBusy(true);
    setError(undefined);
    try {
      const i = await invoiceApi.create(quotationId);
      void navigate('/admin/invoices/' + i.id);
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className={styles.panel}>
      <h2>{t.title}</h2>
      {state.error !== undefined ? (
        <PaymentError
          status={state.error}
          retry={() => setRevision((v) => v + 1)}
        />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : state.data.id ? (
        <Link to={'/admin/invoices/' + state.data.id}>{t.open}</Link>
      ) : (
        <>
          <p>{t.immutable}</p>
          <Button disabled={busy} onClick={() => void create()}>
            {busy ? t.busy : t.create}
          </Button>
        </>
      )}
      {error !== undefined && (
        <PaymentError status={error} retry={() => setRevision((v) => v + 1)} />
      )}
    </section>
  );
}
