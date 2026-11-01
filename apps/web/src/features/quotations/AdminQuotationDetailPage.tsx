import { useCallback, useState } from 'react';
import { Link, useParams } from 'react-router';
import { Button } from '../../components/ui/Button';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useApiResource } from '../../hooks/useApiResource';
import { quotationApi } from './api';
import { useQuotationCopy } from './copy';
import { QuotationEditor } from './QuotationEditor';
import { QuotationActions } from './QuotationActions';
import { QuotationHistory } from './QuotationHistory';
import { QuotationSummary, QuotationStatusBadge } from './QuotationSummary';
import { QuotationError } from './QuotationError';
import styles from './Quotations.module.scss';
export function AdminQuotationDetailPage() {
  const { id = '' } = useParams();
  const t = useQuotationCopy();
  usePageMetadata(t.detail);
  const [revision, setRevision] = useState(0);
  const [preview, setPreview] = useState(true);
  const [message, setMessage] = useState('');
  const load = useCallback(
    async (signal: AbortSignal) => {
      const [detail, configuration] = await Promise.all([
        quotationApi.detail(id, signal),
        quotationApi.configuration(signal),
      ]);
      return { detail, configuration };
    },
    [id],
  );
  const state = useApiResource(`${id}-${revision}`, load);
  function refresh(kind: string) {
    setMessage(kind);
    setRevision((v) => v + 1);
  }
  return (
    <>
      <Link to="/admin/quotations">← {t.back}</Link>
      <h1>{t.detail}</h1>
      {message ? (
        <p role="status">
          {message === 'save'
            ? t.saved
            : message === 'send'
              ? t.queued
              : message === 'cancel'
                ? t.cancelled
                : ''}
        </p>
      ) : null}
      {state.error !== undefined ? (
        <QuotationError status={state.error} retry={() => refresh('refresh')} />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          <div className={styles.row}>
            <Link to={'/admin/requests/' + state.data.detail.requestId}>
              {t.request}: {state.data.detail.requestReference}
            </Link>
            <QuotationStatusBadge status={state.data.detail.quotation.status} />
          </div>
          {state.data.detail.quotation.status === 'Draft' ? (
            <QuotationEditor
              key={state.data.detail.versionToken}
              detail={state.data.detail}
              configuration={state.data.configuration}
              onSaved={() => refresh('save')}
            />
          ) : null}
          <QuotationActions
            detail={state.data.detail}
            configuration={state.data.configuration}
            onSaved={refresh}
          />
          {state.data.detail.quotation.status === 'Draft' ? (
            <p>{t.previewHint}</p>
          ) : null}
          <Button
            variant="outline"
            aria-expanded={preview}
            onClick={() => setPreview((v) => !v)}
          >
            {preview ? t.hidePreview : t.preview}
          </Button>
          {preview ? (
            <QuotationSummary quotation={state.data.detail.quotation} />
          ) : null}
          <QuotationHistory id={id} revision={revision} />
        </>
      )}
    </>
  );
}
