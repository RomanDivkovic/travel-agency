import { useCallback, useState } from 'react';
import { Link, useLocation } from 'react-router';
import { Button } from '../../components/ui/Button';
import { useApiResource } from '../../hooks/useApiResource';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { ApiError } from '../../services/apiClient';
import { publicInvoiceApi } from './api';
import { usePaymentCopy } from './copy';
import { PaymentShell } from './CustomerInvoicePage';
import { useMoney } from './useMoney';
import { PaymentError } from './PaymentError';
import styles from './Payments.module.scss';
export function SandboxPaymentPage() {
  const t = usePaymentCopy();
  const money = useMoney();
  usePageMetadata(t.sandboxTitle);
  const { hash } = useLocation();
  const token = hash.slice(1);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  const [done, setDone] = useState(false);
  const load = useCallback(
    (signal: AbortSignal) =>
      /^[A-Za-z0-9_-]{43}$/.test(token)
        ? publicInvoiceApi.sandbox(token, signal)
        : Promise.reject(new ApiError('Unavailable', 404)),
    [token],
  );
  const state = useApiResource(token, load);
  async function simulate(outcome: string) {
    if (busy || done) return;
    setBusy(true);
    setError(undefined);
    try {
      await publicInvoiceApi.simulate(token, outcome);
      setDone(true);
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  let resultPath: string | undefined;
  if (state.data) {
    try {
      const url = new URL(state.data.resultUrl);
      if (
        url.origin === window.location.origin &&
        url.pathname === '/payment/result'
      )
        resultPath = url.pathname + url.hash;
    } catch {
      /* Invalid destinations are never followed. */
    }
  }
  return (
    <PaymentShell>
      <h1>{t.sandboxTitle}</h1>
      <p className={styles.notice}>{t.simulation}</p>
      <p>{t.sandboxInfo}</p>
      {state.error !== undefined ? (
        <PaymentError status={state.error} />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <section className={styles.panel}>
          <h2>{money(state.data.amountMinorUnits, state.data.currency)}</h2>
          {done || state.data.status !== 'Pending' ? (
            <p role="status">{t.simulatedDone}</p>
          ) : (
            <div className={styles.actions}>
              <Button
                disabled={busy}
                onClick={() => void simulate('Succeeded')}
              >
                {t.simulateSuccess}
              </Button>
              <Button
                variant="outline"
                disabled={busy}
                onClick={() => void simulate('Failed')}
              >
                {t.simulateFailure}
              </Button>
              <Button
                variant="outline"
                disabled={busy}
                onClick={() => void simulate('Cancelled')}
              >
                {t.simulateCancel}
              </Button>
            </div>
          )}
          {error !== undefined && <PaymentError status={error} />}
          {resultPath && <Link to={resultPath}>{t.result}</Link>}
        </section>
      )}
    </PaymentShell>
  );
}
