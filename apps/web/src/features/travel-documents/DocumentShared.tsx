import { useEffect, useRef, useState, type ReactNode } from 'react';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { useDocumentCopy } from './copy';
import { documentApi, saveBlob } from './api';
import type { DocumentVersion } from './types';
import styles from './Documents.module.scss';
export function DocumentError({
  status,
  retry,
}: {
  status: number;
  retry?: () => void;
}) {
  const t = useDocumentCopy();
  return (
    <div role="alert">
      <p>
        {status === 400
          ? t.invalid
          : status === 404
            ? t.unavailable
            : status === 409
              ? t.conflict
              : status === 412
                ? t.configuration
                : status === 413
                  ? t.tooLarge
                  : status === 415
                    ? t.fileError
                    : status === 429
                      ? t.limited
                      : t.error}
      </p>
      {retry && (
        <Button onClick={retry} variant="outline">
          {t.retry}
        </Button>
      )}
    </div>
  );
}
export function DocumentLoad({
  error,
  loading,
  retry,
  children,
}: {
  error?: number;
  loading: boolean;
  retry: () => void;
  children: ReactNode;
}) {
  const t = useDocumentCopy();
  return error !== undefined ? (
    <DocumentError status={error} retry={retry} />
  ) : loading ? (
    <p role="status">{t.loading}</p>
  ) : (
    <>{children}</>
  );
}
export function ConfirmChange({
  label,
  hint,
  onConfirm,
}: {
  label: string;
  hint?: string;
  onConfirm: () => Promise<void>;
}) {
  const t = useDocumentCopy();
  const [open, setOpen] = useState(false),
    [busy, setBusy] = useState(false),
    [error, setError] = useState<number>();
  const opener = useRef<HTMLButtonElement>(null),
    confirm = useRef<HTMLButtonElement>(null),
    restore = useRef(false);
  useEffect(() => {
    if (open) {
      confirm.current?.focus();
      restore.current = true;
    } else if (restore.current) {
      opener.current?.focus();
      restore.current = false;
    }
  }, [open]);
  return (
    <div>
      {!open ? (
        <Button
          ref={opener}
          variant="outline"
          onClick={() => {
            setError(undefined);
            setOpen(true);
          }}
        >
          {label}
        </Button>
      ) : (
        <section className={styles.notice} aria-label={label}>
          <p>{hint ?? t.confirm}</p>
          <div className={styles.actions}>
            <Button
              ref={confirm}
              disabled={busy}
              onClick={() => {
                setBusy(true);
                void onConfirm()
                  .then(() => setOpen(false))
                  .catch((e: unknown) =>
                    setError(e instanceof ApiError ? (e.status ?? 0) : 0),
                  )
                  .finally(() => setBusy(false));
              }}
            >
              {busy ? t.saving : label}
            </Button>
            <Button
              disabled={busy}
              variant="text"
              onClick={() => setOpen(false)}
            >
              {t.close}
            </Button>
          </div>
        </section>
      )}
      {error !== undefined && <DocumentError status={error} />}
    </div>
  );
}
export function FileActions({
  bookingId,
  id,
  version,
  admin,
}: {
  bookingId: string;
  id: string;
  version: DocumentVersion;
  admin: boolean;
}) {
  const t = useDocumentCopy();
  const [busy, setBusy] = useState(false),
    [error, setError] = useState<number>(),
    [preview, setPreview] = useState('');
  useEffect(
    () => () => {
      if (preview) URL.revokeObjectURL(preview);
    },
    [preview],
  );
  async function run(show: boolean) {
    setBusy(true);
    setError(undefined);
    try {
      const blob = await documentApi.file(
        bookingId,
        id,
        admin,
        version.version,
      );
      if (show)
        setPreview(
          URL.createObjectURL(new Blob([blob], { type: version.contentType })),
        );
      else saveBlob(blob, version.originalFileName);
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      <div className={styles.actions}>
        <Button disabled={busy} onClick={() => void run(false)}>
          {busy ? t.loading : t.download}
        </Button>
        {admin && (
          <Button
            disabled={busy}
            variant="outline"
            onClick={() => void run(true)}
          >
            {t.preview}
          </Button>
        )}
      </div>
      {error !== undefined && <DocumentError status={error} />}
      {preview && (
        <section className={styles.preview}>
          <p>{t.previewHint}</p>
          {version.contentType === 'application/pdf' ? (
            <iframe
              sandbox="allow-scripts"
              src={preview}
              title={version.name}
              referrerPolicy="no-referrer"
            />
          ) : (
            <img src={preview} alt={version.name} />
          )}
          <Button variant="text" onClick={() => setPreview('')}>
            {t.close}
          </Button>
        </section>
      )}
    </>
  );
}
