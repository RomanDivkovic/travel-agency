import { useId, useState, type FormEvent } from 'react';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { Field } from '../customer-portal/PortalShared';
import { documentApi } from './api';
import { useDocumentCopy } from './copy';
import { DocumentError } from './DocumentShared';
import {
  documentTypes,
  type DocumentLibrary,
  type DocumentMetadata,
  type TravelDocument,
} from './types';
import styles from './Documents.module.scss';
export function DocumentUpload({
  bookingId,
  library,
  replace,
  onSaved,
}: {
  bookingId: string;
  library: DocumentLibrary;
  replace?: TravelDocument;
  onSaved: () => void;
}) {
  const t = useDocumentCopy();
  const current = replace?.versions.find(
    (v) => v.version === replace.currentVersion,
  );
  const [data, setData] = useState<DocumentMetadata>({
    name: current?.name ?? '',
    description: current?.description ?? '',
    type: replace?.type ?? 'Other',
    reservationId: replace?.reservationId ?? null,
    noIdentityDocuments: false,
  });
  const [file, setFile] = useState<File>(),
    [busy, setBusy] = useState(false),
    [error, setError] = useState<number>();
  const fileId = useId(),
    descriptionId = useId(),
    typeId = useId(),
    reservationId = useId();
  async function submit(e: FormEvent) {
    e.preventDefault();
    if (busy || !file) return;
    setBusy(true);
    setError(undefined);
    try {
      if (file.size > library.maxBytes) throw new ApiError('size', 413);
      await documentApi.upload(
        bookingId,
        file,
        data,
        replace
          ? { id: replace.id, versionToken: replace.versionToken! }
          : undefined,
      );
      onSaved();
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className={styles.panel}>
      <h2>{replace ? t.replace : t.upload}</h2>
      <p>
        {t.uploadHint} {Math.floor(library.maxBytes / 1024 / 1024)} MB
      </p>
      <form className={styles.form} onSubmit={(e) => void submit(e)}>
        <fieldset disabled={busy}>
          <Field
            label={t.name}
            required
            maxLength={150}
            value={data.name}
            onChange={(e) => setData({ ...data, name: e.target.value })}
          />
          <label htmlFor={descriptionId}>
            {t.description}
            <textarea
              id={descriptionId}
              maxLength={2000}
              rows={3}
              value={data.description}
              onChange={(e) =>
                setData({ ...data, description: e.target.value })
              }
            />
          </label>
          <label htmlFor={typeId}>
            {t.type}
            <select
              id={typeId}
              disabled={!!replace}
              value={data.type}
              onChange={(e) =>
                setData({
                  ...data,
                  type: e.target.value as DocumentMetadata['type'],
                })
              }
            >
              {documentTypes.map((type) => (
                <option key={type} value={type}>
                  {t.categories[type]}
                </option>
              ))}
            </select>
          </label>
          <label htmlFor={reservationId}>
            {t.reservation}
            <select
              id={reservationId}
              disabled={!!replace}
              value={data.reservationId ?? ''}
              onChange={(e) =>
                setData({ ...data, reservationId: e.target.value || null })
              }
            >
              <option value="">{t.none}</option>
              {library.reservations.map((r) => (
                <option value={r.id} key={r.id}>
                  {r.title}
                </option>
              ))}
            </select>
          </label>
          <label htmlFor={fileId}>
            {t.file}
            <input
              id={fileId}
              type="file"
              required
              accept="application/pdf,image/jpeg,image/png"
              onChange={(e) => setFile(e.target.files?.[0])}
            />
          </label>
          <label className={styles.check}>
            <input
              type="checkbox"
              required
              checked={data.noIdentityDocuments}
              onChange={(e) =>
                setData({ ...data, noIdentityDocuments: e.target.checked })
              }
            />
            {t.noIdentity}
          </label>
          <Button type="submit" disabled={busy || !file}>
            {busy ? t.saving : replace ? t.replace : t.upload}
          </Button>
        </fieldset>
        {error !== undefined && <DocumentError status={error} />}
      </form>
    </section>
  );
}
