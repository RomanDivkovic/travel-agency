import { useCallback, useState } from 'react';
import { Button } from '../../components/ui/Button';
import { useApiResource } from '../../hooks/useApiResource';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { Pagination } from '../admin/AdminShared';
import { TextField } from '../travel-research/ResearchShared';
import { bookingApi } from './api';
import { useBookingCopy } from './copy';
import { BookingError } from './BookingShared';
import { bookingStatuses } from './types';
import styles from './Booking.module.scss';
import { BookingCards } from './BookingCards';
export function AdminBookingListPage() {
  const t = useBookingCopy();
  usePageMetadata(t.title);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('');
  const [search, setSearch] = useState('');
  const [applied, setApplied] = useState('');
  const [sort, setSort] = useState('updated');
  const [retry, setRetry] = useState(0);
  const load = useCallback(
    (signal: AbortSignal) =>
      bookingApi.list(
        new URLSearchParams({
          page: String(page),
          status,
          search: applied,
          sort,
        }),
        signal,
      ),
    [page, status, applied, sort],
  );
  const state = useApiResource(
    `${page}-${status}-${applied}-${sort}-${retry}`,
    load,
  );
  return (
    <>
      <h1>{t.title}</h1>
      <form
        className={styles.form}
        onSubmit={(e) => {
          e.preventDefault();
          setPage(1);
          setApplied(search);
        }}
      >
        <div className={styles.grid}>
          <TextField
            required={false}
            label={t.search}
            value={search}
            onChange={setSearch}
            maxLength={100}
          />
          <label>
            {t.status}
            <select
              aria-label={t.status}
              value={status}
              onChange={(e) => {
                setStatus(e.target.value);
                setPage(1);
              }}
            >
              <option value="">—</option>
              {bookingStatuses.map((s) => (
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
              value={sort}
              onChange={(e) => {
                setSort(e.target.value);
                setPage(1);
              }}
            >
              <option value="updated">{t.updated}</option>
              <option value="departure">{t.departure}</option>
              <option value="reference">{t.reference}</option>
            </select>
          </label>
        </div>
        <Button type="submit">{t.search}</Button>
      </form>
      {state.error !== undefined ? (
        <BookingError
          status={state.error}
          retry={() => setRetry((v) => v + 1)}
        />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <section className={styles.panel}>
          <h2>{t.title}</h2>
          {state.data.items.length ? (
            <BookingCards items={state.data.items} />
          ) : (
            <p>{t.empty}</p>
          )}
          <Pagination {...state.data} onPage={setPage} />
        </section>
      )}
    </>
  );
}
