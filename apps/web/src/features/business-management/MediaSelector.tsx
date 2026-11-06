import { useCallback, useState } from 'react';
import { useApiResource } from '../../hooks/useApiResource';
import { cmsApi } from './api';
import { Select } from './Fields';
import { useCmsCopy } from './copy';
import type { ContentImage, MediaItem } from './types';
import styles from './Management.module.scss';
export function MediaSelector({
  image,
  initial,
  onChange,
}: {
  image: ContentImage | null;
  initial: MediaItem[];
  onChange: (i: ContentImage | null) => void;
}) {
  const t = useCmsCopy();
  const [page, setPage] = useState(1);
  const load = useCallback(
    (s: AbortSignal) =>
      page === 1 ? Promise.resolve(initial) : cmsApi.media(page, s),
    [page, initial],
  );
  const state = useApiResource(String(page), load);
  const media = state.data || [];
  return (
    <>
      <Select
        label={t.selectImage}
        value={image?.id || ''}
        onChange={(v) => {
          const m = media.find((x) => x.id === v);
          if (!v) onChange(null);
          else if (m)
            onChange({
              id: m.id,
              altSv: m.altSv,
              altEn: m.altEn,
              decorative: m.decorative,
            });
        }}
      >
        <option value="">{t.noImage}</option>
        {image && !media.some((m) => m.id === image.id) && (
          <option value={image.id}>{image.id}</option>
        )}
        {media
          .filter((m) => !m.archived)
          .map((m) => (
            <option key={m.id} value={m.id}>
              {m.fileName}
            </option>
          ))}
      </Select>
      {state.error !== undefined && <p role="alert">{t.error}</p>}
      <div className={styles.actions}>
        <button
          type="button"
          disabled={page === 1}
          onClick={() => setPage((x) => x - 1)}
        >
          {t.previous}
        </button>
        <span>{page}</span>
        <button
          type="button"
          disabled={!state.data || media.length < 20}
          onClick={() => setPage((x) => x + 1)}
        >
          {t.next}
        </button>
      </div>
    </>
  );
}
