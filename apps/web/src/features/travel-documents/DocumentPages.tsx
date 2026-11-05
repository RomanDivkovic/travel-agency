import { useCallback, useState } from 'react';
import { Link, useParams } from 'react-router';
import { Button } from '../../components/ui/Button';
import { useApiResource } from '../../hooks/useApiResource';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useLanguage } from '../../localization/useLanguage';
import { ApiError } from '../../services/apiClient';
import { CustomerPagination } from '../customer-portal/PortalShared';
import { documentApi, saveBlob } from './api';
import { useDocumentCopy } from './copy';
import { DocumentLoad, DocumentError, ConfirmChange } from './DocumentShared';
import { DocumentUpload } from './DocumentUpload';
import { DocumentCard } from './DocumentCard';
import { ItineraryEditor } from './ItineraryEditor';
import { ItineraryTimeline } from './ItineraryTimeline';
import styles from './Documents.module.scss';
function BookingLinks({ id, admin }: { id: string; admin: boolean }) {
  const t = useDocumentCopy();
  const base = (admin ? '/admin' : '/account') + '/bookings/' + id;
  return (
    <nav className={styles.actions}>
      <Link to={base}>← {t.back}</Link>
      <Link to={base + '/documents'}>
        {admin ? t.manageDocuments : t.documents}
      </Link>
      <Link to={base + '/itinerary'}>{t.itinerary}</Link>
    </nav>
  );
}
function Library({ id, admin }: { id: string; admin: boolean }) {
  const t = useDocumentCopy();
  usePageMetadata(admin ? t.manageDocuments : t.documents);
  const [page, setPage] = useState(1),
    [revision, setRevision] = useState(0),
    [saved, setSaved] = useState(false);
  const load = useCallback(
    (signal: AbortSignal) => documentApi.library(id, admin, page, signal),
    [id, admin, page],
  );
  const state = useApiResource(`${id}-${page}-${revision}`, load);
  const d = state.data;
  const refresh = () => {
    setSaved(true);
    setRevision((n) => n + 1);
  };
  return (
    <>
      <BookingLinks id={id} admin={admin} />
      <h1>{admin ? t.manageDocuments : t.documents}</h1>
      {saved && <p role="status">{t.saved}</p>}
      <p>{admin ? t.adminPrivacy : t.privacy}</p>
      <DocumentLoad
        error={state.error}
        loading={!d}
        retry={() => setRevision((n) => n + 1)}
      >
        {d && (
          <>
            <p>{d.reference}</p>
            {admin && (
              <DocumentUpload
                key={revision}
                bookingId={id}
                library={d}
                onSaved={refresh}
              />
            )}
            {d.items.length ? (
              d.items.map((doc) => (
                <DocumentCard
                  key={doc.id + '-' + (doc.versionToken ?? doc.currentVersion)}
                  document={doc}
                  library={d}
                  admin={admin}
                  onSaved={refresh}
                />
              ))
            ) : (
              <p>{t.empty}</p>
            )}
            <CustomerPagination
              page={d.page}
              totalPages={d.totalPages}
              totalCount={d.totalCount}
              onPage={setPage}
            />
          </>
        )}
      </DocumentLoad>
    </>
  );
}
export function DocumentsPage({ admin = false }: { admin?: boolean }) {
  const { id = '' } = useParams();
  return <Library key={id + '-' + admin} id={id} admin={admin} />;
}
function ItineraryView({ id, admin }: { id: string; admin: boolean }) {
  const t = useDocumentCopy();
  const { language } = useLanguage();
  usePageMetadata(admin ? t.manageItinerary : t.itinerary);
  const [revision, setRevision] = useState(0),
    [selected, setSelected] = useState<number>(),
    [preview, setPreview] = useState(false),
    [saved, setSaved] = useState(false),
    [busy, setBusy] = useState(false),
    [error, setError] = useState<number>();
  const load = useCallback(
    (s: AbortSignal) => documentApi.itinerary(id, admin, s),
    [id, admin],
  );
  const state = useApiResource(`${id}-${revision}`, load),
    d = state.data;
  const v =
    d?.versions.find((v) => v.version === (selected ?? d.currentVersion)) ??
    d?.versions[0];
  const refresh = () => {
    setSaved(true);
    setSelected(undefined);
    setRevision((n) => n + 1);
  };
  async function action(op: string) {
    await documentApi.itineraryAction(id, op, d?.versionToken);
    refresh();
  }
  return (
    <>
      <BookingLinks id={id} admin={admin} />
      <h1>{admin ? t.manageItinerary : t.itinerary}</h1>
      {saved && <p role="status">{t.saved}</p>}
      {state.error === 404 ? (
        <section className={styles.panel}>
          <p>{t.noItinerary}</p>
          {admin && (
            <Button
              disabled={busy}
              onClick={() => {
                setBusy(true);
                setError(undefined);
                void action('create')
                  .catch((e: unknown) =>
                    setError(e instanceof ApiError ? (e.status ?? 0) : 0),
                  )
                  .finally(() => setBusy(false));
              }}
            >
              {busy ? t.saving : t.create}
            </Button>
          )}
        </section>
      ) : (
        <DocumentLoad
          error={state.error}
          loading={!d}
          retry={() => setRevision((n) => n + 1)}
        >
          {d && v && (
            <>
              <p>
                {d.reference} · {t.version} {v.version} · {t.statuses[v.status]}
              </p>
              {d.sourceChanged && (
                <p className={styles.notice}>{t.sourceChanged}</p>
              )}
              {admin && (
                <section className={styles.panel}>
                  <label>
                    {t.history}
                    <select
                      aria-label={t.history}
                      value={v.version}
                      onChange={(e) => setSelected(Number(e.target.value))}
                    >
                      {d.versions.map((version) => (
                        <option key={version.id} value={version.version}>
                          {t.version} {version.version} ·{' '}
                          {t.statuses[version.status]}
                        </option>
                      ))}
                    </select>
                  </label>
                  <div className={styles.actions}>
                    <Button
                      variant="outline"
                      onClick={() => setPreview(!preview)}
                    >
                      {t.previewItinerary}
                    </Button>
                    {v.version === d.currentVersion &&
                      (v.status === 'Draft' ? (
                        <>
                          <ConfirmChange
                            label={t.generate}
                            hint={t.generateHint}
                            onConfirm={() => action('generate')}
                          />
                          <ConfirmChange
                            label={t.publish}
                            onConfirm={() => action('publish')}
                          />
                        </>
                      ) : (
                        <ConfirmChange
                          label={t.revise}
                          onConfirm={() => action('revise')}
                        />
                      ))}
                    {v.version === d.currentVersion &&
                      v.status !== 'Archived' && (
                        <ConfirmChange
                          label={t.archive}
                          onConfirm={() => action('archive')}
                        />
                      )}
                  </div>
                </section>
              )}
              {admin &&
                !preview &&
                v.status === 'Draft' &&
                v.version === d.currentVersion && (
                  <section className={styles.panel}>
                    <ItineraryEditor
                      key={v.id + '-' + d.versionToken}
                      data={d}
                      version={v}
                      onSaved={refresh}
                    />
                  </section>
                )}
              <ItineraryTimeline version={v} />
              <Button
                disabled={busy}
                onClick={() => {
                  setBusy(true);
                  setError(undefined);
                  void documentApi
                    .pdf(id, admin, language, v.version)
                    .then((blob) =>
                      saveBlob(
                        blob,
                        d.reference + '-itinerary-v' + v.version + '.pdf',
                      ),
                    )
                    .catch((e: unknown) =>
                      setError(e instanceof ApiError ? (e.status ?? 0) : 0),
                    )
                    .finally(() => setBusy(false));
                }}
              >
                {busy ? t.loading : t.pdf}
              </Button>
              <p>
                <Link
                  to={
                    (admin ? '/admin' : '/account') +
                    '/bookings/' +
                    id +
                    '/documents'
                  }
                >
                  {t.related}
                </Link>
              </p>
            </>
          )}
        </DocumentLoad>
      )}
      {error !== undefined && <DocumentError status={error} />}
    </>
  );
}
export function ItineraryPage({ admin = false }: { admin?: boolean }) {
  const { id = '' } = useParams();
  return <ItineraryView key={id + '-' + admin} id={id} admin={admin} />;
}
