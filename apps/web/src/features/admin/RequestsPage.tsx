import { useCallback, useState, type FormEvent } from 'react';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { Button } from '../../components/ui/Button';
import { useWizardCopy } from '../travel-request/copy';
import { services } from '../travel-request/model';
import { adminApi } from './api';
import { initialFilters, statuses, type Filters } from './model';
import { useAdminCopy } from './copy';
import { useAdminResource } from './useAdminResource';
import { ErrorState, Pagination, RequestCards } from './AdminShared';
import styles from './Admin.module.scss';
export function RequestsPage() {
  const t = useAdminCopy();
  const w = useWizardCopy();
  usePageMetadata(t.requests);
  const [filters, setFilters] = useState<Filters>(initialFilters);
  const [draft, setDraft] = useState<Filters>(initialFilters);
  const [revision, setRevision] = useState(0);
  const load = useCallback(
    (signal: AbortSignal) => adminApi.list(filters, signal),
    [filters],
  );
  const state = useAdminResource(JSON.stringify([filters, revision]), load);
  function apply(e: FormEvent) {
    e.preventDefault();
    setFilters({ ...draft, page: 1 });
  }
  function update<K extends keyof Filters>(key: K, value: Filters[K]) {
    setDraft((d) => ({ ...d, [key]: value }));
  }
  return (
    <>
      <h1>{t.requests}</h1>
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
            {statuses.map((s) => (
              <option key={s} value={s}>
                {t.statuses[s]}
              </option>
            ))}
          </select>
        </label>
        <label>
          {t.service}
          <select
            aria-label={t.service}
            value={draft.serviceType}
            onChange={(e) => update('serviceType', e.target.value)}
          >
            <option value="">{t.all}</option>
            {services.map((s) => (
              <option key={s} value={s}>
                {w.serviceNames[s]}
              </option>
            ))}
          </select>
        </label>
        <label>
          {t.from}
          <input
            type="date"
            value={draft.from}
            max={draft.to || undefined}
            onChange={(e) => update('from', e.target.value)}
          />
        </label>
        <label>
          {t.to}
          <input
            type="date"
            value={draft.to}
            min={draft.from || undefined}
            onChange={(e) => update('to', e.target.value)}
          />
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
        <ErrorState
          status={state.error}
          retry={() => setRevision((v) => v + 1)}
        />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <p role="status">
            {state.data.totalCount} {t.results}
          </p>
          <RequestCards items={state.data.items} />
          <Pagination
            {...state.data}
            onPage={(page) => setFilters((f) => ({ ...f, page }))}
          />
        </>
      )}
    </>
  );
}
