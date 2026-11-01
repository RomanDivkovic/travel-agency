import { useCallback, useState, type FormEvent } from 'react';
import { Button } from '../../components/ui/Button';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useApiResource } from '../../hooks/useApiResource';
import { Pagination } from '../admin/AdminShared';
import { quotationApi } from './api';
import { useQuotationCopy } from './copy';
import { quotationStatuses, type QuotationFilters } from './types';
import { QuotationCards } from './QuotationCards';
import { QuotationError } from './QuotationError';
import styles from './Quotations.module.scss';
const defaults: QuotationFilters = {
  page: 1,
  pageSize: 20,
  status: '',
  search: '',
  sort: 'createdAt_desc',
};
export function AdminQuotationListPage() {
  const t = useQuotationCopy();
  usePageMetadata(t.title);
  const [filters, setFilters] = useState(defaults);
  const [draft, setDraft] = useState(defaults);
  const [revision, setRevision] = useState(0);
  const load = useCallback(
    (signal: AbortSignal) => quotationApi.list(filters, signal),
    [filters],
  );
  const state = useApiResource(JSON.stringify([filters, revision]), load);
  function update<K extends keyof QuotationFilters>(
    key: K,
    value: QuotationFilters[K],
  ) {
    setDraft((d) => ({ ...d, [key]: value }));
  }
  function apply(e: FormEvent) {
    e.preventDefault();
    setFilters({ ...draft, page: 1 });
  }
  return (
    <>
      <h1>{t.title}</h1>
      <form className={styles.filters} onSubmit={apply}>
        <label className={styles.search}>
          {t.search}
          <input
            type="search"
            maxLength={150}
            value={draft.search}
            onChange={(e) => update('search', e.target.value)}
          />
        </label>
        <label>
          {t.status}
          <select
            aria-label={t.status}
            value={draft.status}
            onChange={(e) => update('status', e.target.value)}
          >
            <option value="">{t.all}</option>
            {quotationStatuses.map((s) => (
              <option key={s} value={s}>
                {t.statuses[s]}
              </option>
            ))}
          </select>
        </label>
        <label>
          {t.sort}
          <select
            aria-label={t.sort}
            value={draft.sort}
            onChange={(e) => update('sort', e.target.value)}
          >
            <option value="createdAt_desc">{t.newest}</option>
            <option value="createdAt_asc">{t.oldest}</option>
            <option value="updatedAt_desc">{t.updated}</option>
          </select>
        </label>
        <label>
          {t.pageSize}
          <select
            aria-label={t.pageSize}
            value={draft.pageSize}
            onChange={(e) => update('pageSize', Number(e.target.value))}
          >
            {[10, 20, 50, 100].map((n) => (
              <option key={n}>{n}</option>
            ))}
          </select>
        </label>
        <Button type="submit">{t.apply}</Button>
      </form>
      {state.error !== undefined ? (
        <QuotationError
          status={state.error}
          retry={() => setRevision((v) => v + 1)}
        />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <QuotationCards items={state.data.items} />
          <Pagination
            {...state.data}
            onPage={(page) => setFilters((f) => ({ ...f, page }))}
          />
        </>
      )}
    </>
  );
}
