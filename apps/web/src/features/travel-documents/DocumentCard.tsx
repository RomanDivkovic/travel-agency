import { useId, useState } from 'react';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { useDateFormat } from '../admin/useDateFormat';
import { Field } from '../customer-portal/PortalShared';
import { documentApi } from './api';
import { useDocumentCopy } from './copy';
import { ConfirmChange, DocumentError, FileActions } from './DocumentShared';
import { DocumentUpload } from './DocumentUpload';
import type { DocumentLibrary, TravelDocument } from './types';
import styles from './Documents.module.scss';
export function DocumentCard({
  document: d,
  library,
  admin,
  onSaved,
}: {
  document: TravelDocument;
  library: DocumentLibrary;
  admin: boolean;
  onSaved: () => void;
}) {
  const t = useDocumentCopy(),
    date = useDateFormat();
  const [replace, setReplace] = useState(false),
    [edit, setEdit] = useState(false);
  const current = d.versions.find((v) => v.version === d.currentVersion)!;
  const [name, setName] = useState(current.name),
    [description, setDescription] = useState(current.description),
    [busy, setBusy] = useState(false),
    [error, setError] = useState<number>();
  const descId = useId();
  return (
    <article className={styles.panel}>
      <h2>{current.name || t.statuses.Deleted}</h2>
      <p>
        {t.categories[d.type]}
        {d.reservationTitle ? ' · ' + d.reservationTitle : ''}
      </p>
      <p>{current.description}</p>
      <p>
        {current.contentType} · {Math.ceil(current.size / 1024)} KB
      </p>
      <p>
        <strong>{t.statuses[current.status]}</strong> · {t.version}{' '}
        {current.version}
      </p>
      {current.publishedAt && (
        <p>
          {t.publishedAt}: {date(current.publishedAt)}
        </p>
      )}
      {admin && <p>{t.statuses[current.scan]}</p>}
      {current.scan === 'Quarantined' && admin && (
        <p className={styles.notice}>{t.quarantine}</p>
      )}
      {current.scan === 'Infected' && admin && <p role="alert">{t.infected}</p>}
      {current.scan === 'Clean' && current.status !== 'Deleted' && (
        <FileActions
          bookingId={d.bookingId}
          id={d.id}
          version={current}
          admin={admin}
        />
      )}
      {admin && (
        <>
          <div className={styles.actions}>
            {current.status === 'Draft' && (
              <>
                <Button variant="outline" onClick={() => setEdit(!edit)}>
                  {t.edit}
                </Button>
                <ConfirmChange
                  label={t.scan}
                  onConfirm={async () => {
                    await documentApi.change(
                      d.bookingId,
                      d.id,
                      'scan',
                      d.versionToken!,
                      current.version,
                    );
                    onSaved();
                  }}
                />
                {current.scan === 'Clean' && (
                  <ConfirmChange
                    label={t.publish}
                    onConfirm={async () => {
                      await documentApi.change(
                        d.bookingId,
                        d.id,
                        'publish',
                        d.versionToken!,
                        current.version,
                      );
                      onSaved();
                    }}
                  />
                )}
              </>
            )}
            {current.status !== 'Deleted' && (
              <Button variant="outline" onClick={() => setReplace(!replace)}>
                {t.replace}
              </Button>
            )}
          </div>
          {edit && (
            <form
              className={styles.form}
              onSubmit={(e) => {
                e.preventDefault();
                setBusy(true);
                setError(undefined);
                void documentApi
                  .edit(d.bookingId, d.id, d.versionToken!, name, description)
                  .then(onSaved)
                  .catch((e: unknown) =>
                    setError(e instanceof ApiError ? (e.status ?? 0) : 0),
                  )
                  .finally(() => setBusy(false));
              }}
            >
              <Field
                label={t.name}
                value={name}
                required
                maxLength={150}
                onChange={(e) => setName(e.target.value)}
              />
              <label htmlFor={descId}>
                {t.description}
                <textarea
                  id={descId}
                  value={description}
                  maxLength={2000}
                  rows={3}
                  onChange={(e) => setDescription(e.target.value)}
                />
              </label>
              <Button type="submit" disabled={busy}>
                {busy ? t.saving : t.save}
              </Button>
              {error !== undefined && <DocumentError status={error} />}
            </form>
          )}
          {replace && (
            <DocumentUpload
              bookingId={d.bookingId}
              library={library}
              replace={d}
              onSaved={onSaved}
            />
          )}
          <details className={styles.history}>
            <summary>{t.history}</summary>
            {d.versions.map((v) => (
              <section key={v.id} className={styles.panel}>
                <h3>
                  {t.version} {v.version} · {t.statuses[v.status]}
                </h3>
                <p>{v.name}</p>
                <p>
                  {date(v.createdAt)} · {t.statuses[v.scan]}
                </p>
                {v.scan === 'Clean' &&
                  v.status !== 'Deleted' &&
                  v.version !== current.version && (
                    <FileActions
                      bookingId={d.bookingId}
                      id={d.id}
                      version={v}
                      admin
                    />
                  )}
                <div className={styles.actions}>
                  {!['Archived', 'Deleted'].includes(v.status) && (
                    <ConfirmChange
                      label={t.archive}
                      onConfirm={async () => {
                        await documentApi.change(
                          d.bookingId,
                          d.id,
                          'archive',
                          d.versionToken!,
                          v.version,
                        );
                        onSaved();
                      }}
                    />
                  )}
                  {v.status !== 'Deleted' && (
                    <ConfirmChange
                      label={t.remove}
                      onConfirm={async () => {
                        await documentApi.change(
                          d.bookingId,
                          d.id,
                          'delete',
                          d.versionToken!,
                          v.version,
                        );
                        onSaved();
                      }}
                    />
                  )}
                </div>
              </section>
            ))}
          </details>
        </>
      )}
    </article>
  );
}
