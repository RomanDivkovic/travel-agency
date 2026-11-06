import { contentError } from './errors';
import { ManagementLinks } from './ManagementLinks';
import { useCallback, useState, useRef, type FormEvent } from 'react';
import { Link, useParams, useNavigate } from 'react-router';
import { useApiResource } from '../../hooks/useApiResource';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { cmsApi } from './api';
import { useCmsCopy, entityLabel } from './copy';
import { useLanguage } from '../../localization/useLanguage';
import { BusinessEditor, PreferencesEditor } from './BusinessEditor';
import {
  ServiceEditor,
  PageEditor,
  FaqEditor,
  PolicyEditor,
} from './ContentEditors';
import { Check } from './Fields';
import type {
  CmsData,
  ContentDetail,
  MediaItem,
  BusinessData,
  PreferencesData,
  ServiceData,
  PageData,
  FaqData,
  PolicyData,
} from './types';
import styles from './Management.module.scss';
export function ManagementPage({
  kind = 'pages',
}: {
  kind?: 'pages' | 'services' | 'faqs' | 'policies';
}) {
  const t = useCmsCopy();
  usePageMetadata(t[kind]);
  const navigate = useNavigate();
  const { language } = useLanguage();
  const load = useCallback((s: AbortSignal) => cmsApi.list(s), []);
  const state = useApiResource(kind, load);
  const prefix = {
    pages: 'page:',
    services: 'service:',
    faqs: 'faq:',
    policies: 'policy:',
  }[kind];
  return (
    <div className={styles.page}>
      <h1>{t[kind]}</h1>
      <ManagementLinks />
      {kind === 'services' && <p>{t.note}</p>}
      {kind === 'policies' && <p>{t.legal}</p>}
      {kind === 'faqs' && (
        <button
          onClick={() =>
            navigate(
              '/admin/content/edit/' +
                encodeURIComponent('faq:' + crypto.randomUUID()),
            )
          }
        >
          {t.newFaq}
        </button>
      )}
      {state.error !== undefined ? (
        <p role="alert">{t.error}</p>
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <ul className={styles.list}>
          {state.data
            .filter((d) => d.key.startsWith(prefix))
            .map((d) => (
              <li key={d.key}>
                <Link to={'/admin/content/edit/' + encodeURIComponent(d.key)}>
                  {(language === 'sv' ? d.titleSv : d.titleEn) ||
                    entityLabel(d.key, language === 'sv')}
                </Link>
                <p>
                  {t.published}: {d.publishedVersion || '—'}
                </p>
              </li>
            ))}
        </ul>
      )}
    </div>
  );
}
export function ContentEditorPage({ documentKey }: { documentKey?: string }) {
  const params = useParams();
  const key = documentKey || params.key || '';
  const t = useCmsCopy();
  const [revision, setRevision] = useState(0);
  const [notice, setNotice] = useState('');
  const load = useCallback(
    (s: AbortSignal) =>
      Promise.all([cmsApi.detail(key, s), cmsApi.media(1, s)]),
    [key],
  );
  const state = useApiResource(key + revision, load);
  usePageMetadata(t.content);
  return (
    <div className={styles.page}>
      <h1>
        {key === 'business'
          ? t.business
          : key === 'preferences'
            ? t.preferences
            : t.content}
      </h1>
      <ManagementLinks />
      {notice && <p role="status">{notice}</p>}
      {state.error !== undefined ? (
        <>
          <p role="alert">{t.error}</p>
          <button onClick={() => setRevision((x) => x + 1)}>{t.reload}</button>
        </>
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <EditorForm
          key={key + state.data[0].versionToken}
          detail={state.data[0]}
          media={state.data[1]}
          refresh={(message) => {
            setNotice(message);
            setRevision((x) => x + 1);
          }}
        />
      )}
    </div>
  );
}
function EditorForm({
  detail,
  media,
  refresh,
}: {
  detail: ContentDetail;
  media: MediaItem[];
  refresh: (message: string) => void;
}) {
  const t = useCmsCopy();
  const { language } = useLanguage();
  const [data, setData] = useState<CmsData>(detail.draft);
  const [dirty, setDirty] = useState(false);
  const [confirmed, setConfirmed] = useState(false);
  const [busy, setBusy] = useState(false);
  const pending = useRef(false);
  const [error, setError] = useState<string>();
  const [message, setMessage] = useState('');
  const change = (d: CmsData) => {
    setData(d);
    setDirty(true);
    setConfirmed(false);
    setMessage('');
  };
  async function action(
    kind: 'save' | 'publish' | 'restore',
    version?: number,
  ) {
    if (pending.current) return;
    pending.current = true;
    setBusy(true);
    setError(undefined);
    try {
      if (kind === 'save')
        await cmsApi.save(detail.key, detail.versionToken, data);
      else if (kind === 'publish')
        await cmsApi.publish(detail.key, detail.versionToken);
      else await cmsApi.restore(detail.key, detail.versionToken, version!);
      setMessage(kind === 'save' ? t.saved : t.success);
      refresh(kind === 'save' ? t.saved : t.success);
    } catch (e) {
      setError(contentError(e, t));
    } finally {
      pending.current = false;
      setBusy(false);
    }
  }
  function submit(e: FormEvent) {
    e.preventDefault();
    void action('save');
  }
  const k = detail.key;
  return (
    <>
      <p>
        {entityLabel(k, language === 'sv')} — {t.published}:{' '}
        {detail.publishedVersion || '—'}
      </p>
      {detail.versionToken === '00000000-0000-0000-0000-000000000000' && (
        <p>{t.baseline}</p>
      )}
      <form onSubmit={submit}>
        <fieldset disabled={busy} className={styles.panel}>
          {k === 'business' ? (
            <BusinessEditor data={data as BusinessData} onChange={change} />
          ) : k === 'preferences' ? (
            <PreferencesEditor
              data={data as PreferencesData}
              onChange={change}
            />
          ) : k.startsWith('service:') ? (
            <ServiceEditor data={data as ServiceData} onChange={change} />
          ) : k.startsWith('page:') ? (
            <PageEditor
              data={data as PageData}
              onChange={change}
              media={media}
            />
          ) : k.startsWith('faq:') ? (
            <FaqEditor data={data as FaqData} onChange={change} />
          ) : (
            <PolicyEditor data={data as PolicyData} onChange={change} />
          )}
          <button type="submit">{busy ? t.loading : t.save}</button>
        </fieldset>
      </form>
      {dirty && <p role="status">{t.unsaved}</p>}
      {error && <p role="alert">{error}</p>}
      {message && <p role="status">{message}</p>}
      <section className={styles.panel}>
        <h2>{t.status}</h2>
        {k !== 'preferences' && !dirty && (
          <Link to={'/admin/content/preview/' + encodeURIComponent(k)}>
            {t.preview}
          </Link>
        )}
        <Check label={t.confirm} checked={confirmed} onChange={setConfirmed} />
        <button
          disabled={
            busy ||
            dirty ||
            !confirmed ||
            detail.versionToken === '00000000-0000-0000-0000-000000000000'
          }
          onClick={() => void action('publish')}
        >
          {t.publish}
        </button>
      </section>
      <section className={styles.panel}>
        <h2>{t.history}</h2>
        <p>
          {language === 'sv'
            ? 'De senaste 100 publicerade versionerna visas. Tidigare publiceringar ändras aldrig.'
            : 'Showing the latest 100 published versions. Historical publications are never modified.'}
        </p>
        <ul className={styles.list}>
          {detail.versions.map((v) => (
            <li key={v.version}>
              <details>
                <summary>
                  {t.version} {v.version} ·{' '}
                  {new Date(v.publishedAt).toLocaleString(language)} ·{' '}
                  {v.publishedBy || '—'}
                  {v.version === detail.publishedVersion
                    ? ' · ' + t.published
                    : ''}
                </summary>
                <HistoryData data={v.data} />
              </details>
              <button
                disabled={busy || dirty}
                onClick={() => void action('restore', v.version)}
              >
                {t.restore} {v.version}
              </button>
            </li>
          ))}
        </ul>
      </section>
    </>
  );
}
function HistoryData({ data }: { data: CmsData }) {
  return (
    <dl>
      {Object.entries(data).map(([key, value]) => (
        <div key={key}>
          <dt>{key}</dt>
          <dd style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>
            {typeof value === 'object' && value !== null ? (
              <HistoryData data={value as CmsData} />
            ) : (
              String(value ?? '—')
            )}
          </dd>
        </div>
      ))}
    </dl>
  );
}
