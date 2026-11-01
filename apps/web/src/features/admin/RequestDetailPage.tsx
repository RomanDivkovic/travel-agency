import { useDateFormat } from './useDateFormat';
import { useCallback, useState } from 'react';
import { Link, useParams } from 'react-router';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useWizardCopy } from '../travel-request/copy';
import { useAdminCopy } from './copy';
import { adminApi } from './api';
import { useAdminResource } from './useAdminResource';
import { ErrorState, StatusBadge } from './AdminShared';
import { RequestFields } from './RequestFields';
import { RequestActions } from './RequestActions';
import { RequestTimeline } from './RequestTimeline';
import styles from './Admin.module.scss';
export function RequestDetailPage() {
  const { id = '' } = useParams();
  const t = useAdminCopy();
  const w = useWizardCopy();
  const date = useDateFormat();
  usePageMetadata(t.overview);
  const [revision, setRevision] = useState(0);
  const [message, setMessage] = useState('');
  const load = useCallback(
    (signal: AbortSignal) => adminApi.detail(id, signal),
    [id],
  );
  const state = useAdminResource(`${id}-${revision}`, load);
  const r = state.data;
  return (
    <>
      <Link to="/admin/requests">← {t.back}</Link>
      <h1>{t.overview}</h1>
      {message ? (
        <p role="status">
          {message === 'status' ? t.statusSaved : t.noteSaved}
        </p>
      ) : null}
      {state.error !== undefined ? (
        <ErrorState
          status={state.error}
          retry={() => setRevision((v) => v + 1)}
        />
      ) : !r ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <section className={styles.panel}>
            <div className={styles.cardTop}>
              <h2>{r.referenceNumber}</h2>
              <StatusBadge status={r.status} />
            </div>
            <p>{w.serviceNames[r.serviceType]}</p>
            <p>
              {t.submitted}: {date(r.createdAt)} · {t.updated}:{' '}
              {date(r.updatedAt)}
            </p>
          </section>
          <RequestFields request={r} />
          <RequestActions
            key={r.version}
            request={r}
            onRefresh={() => setRevision((v) => v + 1)}
            onSaved={(kind) => {
              setMessage(kind);
              setRevision((v) => v + 1);
            }}
          />
          <div className={styles.detailGrid}>
            <RequestTimeline id={id} revision={revision} kind="notes" />
            <RequestTimeline id={id} revision={revision} kind="activity" />
          </div>
        </>
      )}
    </>
  );
}
