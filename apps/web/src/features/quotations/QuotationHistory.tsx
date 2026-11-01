import { useCallback, useState } from 'react';
import { useApiResource } from '../../hooks/useApiResource';
import { Pagination } from '../admin/AdminShared';
import { useDateFormat } from '../admin/useDateFormat';
import { quotationApi } from './api';
import { quotationEventLabel, useQuotationCopy } from './copy';
import { QuotationError } from './QuotationError';
import styles from './Quotations.module.scss';
export function QuotationHistory({
  id,
  revision,
}: {
  id: string;
  revision: number;
}) {
  const t = useQuotationCopy();
  const date = useDateFormat();
  const [page, setPage] = useState(1);
  const [retry, setRetry] = useState(0);
  const load = useCallback(
    (signal: AbortSignal) => quotationApi.history(id, page, signal),
    [id, page],
  );
  const state = useApiResource(`${id}-${revision}-${page}-${retry}`, load);
  return (
    <section className={styles.panel}>
      <h2>{t.history}</h2>
      {state.error !== undefined ? (
        <QuotationError
          status={state.error}
          retry={() => setRetry((v) => v + 1)}
        />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          {state.data.items.length ? (
            <ol>
              {state.data.items.map((a) => (
                <li key={a.id}>
                  <p>
                    {quotationEventLabel(a.type, t)}{' '}
                    {a.quotationVersion
                      ? `${t.version} ${a.quotationVersion}`
                      : ''}
                  </p>
                  <small>
                    {a.author ??
                      (a.type === 'QuotationExpired'
                        ? t.system
                        : t.customer)}{' '}
                    · {date(a.createdAt)}
                  </small>
                </li>
              ))}
            </ol>
          ) : (
            <p>{t.noHistory}</p>
          )}
          {state.data.totalPages > 1 ? (
            <Pagination {...state.data} onPage={setPage} />
          ) : null}
        </>
      )}
    </section>
  );
}
