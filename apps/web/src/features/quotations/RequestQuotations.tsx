import { useCallback, useRef, useState } from 'react';
import { useNavigate } from 'react-router';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { useApiResource } from '../../hooks/useApiResource';
import { Pagination } from '../admin/AdminShared';
import { quotationApi } from './api';
import { useQuotationCopy } from './copy';
import { QuotationCards } from './QuotationCards';
import { QuotationError } from './QuotationError';
import styles from './Quotations.module.scss';
export function RequestQuotations({
  requestId,
  closed = false,
}: {
  requestId: string;
  closed?: boolean;
}) {
  const t = useQuotationCopy();
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [revision, setRevision] = useState(0);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<number>();
  const pending = useRef(false);
  const load = useCallback(
    (signal: AbortSignal) => quotationApi.forRequest(requestId, page, signal),
    [requestId, page],
  );
  const state = useApiResource(`${requestId}-${page}-${revision}`, load);
  async function create() {
    if (pending.current) return;
    pending.current = true;
    setBusy(true);
    try {
      const result = await quotationApi.create(requestId);
      await navigate('/admin/quotations/' + result.id);
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
      setRevision((v) => v + 1);
    } finally {
      pending.current = false;
      setBusy(false);
    }
  }
  return (
    <section className={styles.panel}>
      <h2>{t.title}</h2>
      {state.error !== undefined ? (
        <QuotationError
          status={state.error}
          retry={() => setRevision((v) => v + 1)}
        />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <QuotationCards nested items={state.data.items} />
          {state.data.totalPages > 1 ? (
            <Pagination {...state.data} onPage={setPage} />
          ) : null}
          {state.data.totalCount === 0 && !closed ? (
            <Button disabled={busy} onClick={() => void create()}>
              {busy ? t.saving : t.create}
            </Button>
          ) : null}
        </>
      )}
      {error !== undefined ? (
        <QuotationError
          status={error}
          retry={() => {
            setError(undefined);
            setRevision((v) => v + 1);
          }}
        />
      ) : null}
    </section>
  );
}
