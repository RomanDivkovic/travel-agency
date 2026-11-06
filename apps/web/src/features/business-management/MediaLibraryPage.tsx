import { useLanguage } from '../../localization/useLanguage';
import { useCallback, useRef, useState, type FormEvent } from 'react';
import { useApiResource } from '../../hooks/useApiResource';
import { cmsApi, mediaUrl } from './api';
import { useCmsCopy } from './copy';
import { ManagementLinks } from './ManagementLinks';
import { Check, TextField } from './Fields';
import { ApiError } from '../../services/apiClient';
import type { MediaItem } from './types';
import styles from './Management.module.scss';
export function MediaLibraryPage() {
  const t = useCmsCopy();
  const [page, setPage] = useState(1);
  const [revision, setRevision] = useState(0);
  const load = useCallback((s: AbortSignal) => cmsApi.media(page, s), [page]);
  const state = useApiResource(page + '-' + revision, load);
  const [busy, setBusy] = useState(false);
  const pending = useRef(false);
  const [error, setError] = useState(false);
  const [message, setMessage] = useState('');
  const [decorative, setDecorative] = useState(false);
  async function upload(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (pending.current) return;
    const form = e.currentTarget;
    const data = new FormData(form);
    data.set('decorative', String(decorative));
    if (decorative) {
      data.set('altSv', '');
      data.set('altEn', '');
    }
    const file = data.get('file');
    if (!(file instanceof File) || file.size > 3 * 1024 * 1024) {
      setError(true);
      return;
    }
    pending.current = true;
    setBusy(true);
    setError(false);
    try {
      await cmsApi.upload(data);
      form.reset();
      setDecorative(false);
      setRevision((x) => x + 1);
      setMessage(t.success);
    } catch {
      setError(true);
    } finally {
      setBusy(false);
      pending.current = false;
    }
  }
  return (
    <div className={styles.page}>
      <h1>{t.media}</h1>
      <ManagementLinks />
      <form onSubmit={(e) => void upload(e)} className={styles.panel}>
        <fieldset disabled={busy}>
          <label className={styles.field}>
            {t.file}
            <input
              type="file"
              name="file"
              accept="image/png,image/jpeg,image/webp"
              required
            />
          </label>
          <label className={styles.field}>
            {t.altSv}
            <input
              name="altSv"
              maxLength={500}
              required={!decorative}
              disabled={decorative}
            />
          </label>
          <label className={styles.field}>
            {t.altEn}
            <input
              name="altEn"
              maxLength={500}
              required={!decorative}
              disabled={decorative}
            />
          </label>
          <Check
            label={t.decorative}
            checked={decorative}
            onChange={setDecorative}
          />
          <button type="submit">{busy ? t.loading : t.upload}</button>
        </fieldset>
      </form>
      {error && <p role="alert">{t.error}</p>}
      {message && <p role="status">{message}</p>}
      {state.error !== undefined ? (
        <p role="alert">{t.error}</p>
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <div className={styles.grid}>
            {state.data.map((item) => (
              <MediaCard
                key={item.id + item.versionToken}
                item={item}
                refresh={() => setRevision((x) => x + 1)}
              />
            ))}
          </div>
          {!state.data.length && <p>{t.empty}</p>}
          <div className={styles.actions}>
            <button disabled={page === 1} onClick={() => setPage((x) => x - 1)}>
              {t.previous}
            </button>
            <span>{page}</span>
            <button
              disabled={state.data.length < 20}
              onClick={() => setPage((x) => x + 1)}
            >
              {t.next}
            </button>
          </div>
        </>
      )}
    </div>
  );
}
function MediaCard({
  item,
  refresh,
}: {
  item: MediaItem;
  refresh: () => void;
}) {
  const t = useCmsCopy();
  const { language } = useLanguage();
  const [value, setValue] = useState(item);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  async function save(e: FormEvent) {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setError('');
    try {
      await cmsApi.changeMedia(value);
      refresh();
    } catch (e) {
      setError(e instanceof ApiError && e.status === 409 ? t.inUse : t.error);
    } finally {
      setBusy(false);
    }
  }
  return (
    <form onSubmit={(e) => void save(e)} className={styles.panel}>
      <h2>{item.fileName}</h2>
      <img
        className={styles.mediaImage}
        src={mediaUrl(item.id, true)}
        alt={item.decorative ? '' : language === 'sv' ? item.altSv : item.altEn}
      />
      <fieldset disabled={busy}>
        <TextField
          label={t.altSv}
          value={value.altSv}
          required={!value.decorative}
          onChange={(v) => setValue({ ...value, altSv: v })}
        />
        <TextField
          label={t.altEn}
          value={value.altEn}
          required={!value.decorative}
          onChange={(v) => setValue({ ...value, altEn: v })}
        />
        <Check
          label={t.decorative}
          checked={value.decorative}
          onChange={(v) =>
            setValue({
              ...value,
              decorative: v,
              altSv: v ? '' : value.altSv,
              altEn: v ? '' : value.altEn,
            })
          }
        />
        <Check
          label={t.archived}
          checked={value.archived}
          onChange={(v) => setValue({ ...value, archived: v })}
        />
        <button type="submit">{t.save}</button>
      </fieldset>
      {error && <p role="alert">{error}</p>}
    </form>
  );
}
