import { useCallback, useId, useRef, useState, type FormEvent } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { Button } from '../../components/ui/Button';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { ApiError } from '../../services/apiClient';
import { CustomerPagination } from '../customer-portal/PortalShared';
import { messagingApi } from './api';
import { useMessaging } from './context';
import { useMessagingCopy } from './copy';
import { useMessagingResource } from './useMessagingResource';
import { MessageError } from './Shared';
import styles from './Messaging.module.scss';
export function NewConversationPage() {
  const t = useMessagingCopy();
  const navigate = useNavigate(),
    summary = useMessaging();
  usePageMetadata(t.newConversation);
  const [params] = useSearchParams();
  const [subject, setSubject] = useState(''),
    [body, setBody] = useState(''),
    [kind, setKind] = useState(
      params.has('bookingId')
        ? 'bookings'
        : params.has('requestId')
          ? 'requests'
          : '',
    ),
    [related, setRelated] = useState(
      params.get('bookingId') ?? params.get('requestId') ?? '',
    );
  const [clientId, setClientId] = useState(() => crypto.randomUUID());
  const [search, setSearch] = useState(''),
    [query, setQuery] = useState(''),
    [page, setPage] = useState(1),
    [prefill, setPrefill] = useState(related);
  const [busy, setBusy] = useState(false),
    [error, setError] = useState<number>();
  const pending = useRef(false);
  const subjectId = useId(),
    bodyId = useId();
  const loadConfig = useCallback(
    (s: AbortSignal) => messagingApi.configuration(s),
    [],
  );
  const config = useMessagingResource('configuration', loadConfig);
  const loadLinks = useCallback(
    (s: AbortSignal) =>
      kind
        ? messagingApi.links(kind, page, query, prefill, s)
        : Promise.resolve({
            items: [],
            page: 1,
            pageSize: 20,
            totalCount: 0,
            totalPages: 0,
          }),
    [kind, page, query, prefill],
  );
  const links = useMessagingResource(
    kind + '-' + page + '-' + query + '-' + prefill,
    loadLinks,
  );
  const max = config.data?.maxMessageLength;
  function changed() {
    setClientId(crypto.randomUUID());
  }
  async function submit(e: FormEvent) {
    e.preventDefault();
    if (pending.current || !max) return;
    if (!subject.trim() || !body.trim() || (kind && !related)) {
      setError(400);
      return;
    }
    pending.current = true;
    setBusy(true);
    setError(undefined);
    try {
      const saved = await messagingApi.create({
        clientId,
        subject,
        body,
        requestId: kind === 'requests' ? related : null,
        bookingId: kind === 'bookings' ? related : null,
      });
      summary.refresh();
      navigate('/account/messages/' + saved.id);
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      pending.current = false;
      setBusy(false);
    }
  }
  return (
    <>
      <Link to="/account/messages">← {t.back}</Link>
      <h1>{t.newConversation}</h1>
      <p className={styles.notice}>{t.formal}</p>
      {config.error !== undefined && !max ? (
        <MessageError status={config.error} retry={config.refresh} />
      ) : !max ? (
        <p role="status">{t.loading}</p>
      ) : (
        <form
          className={`${styles.form} ${styles.panel}`}
          onSubmit={(e) => void submit(e)}
        >
          <fieldset disabled={busy}>
            <label htmlFor={subjectId}>
              {t.subject}
              <input
                id={subjectId}
                required
                maxLength={150}
                value={subject}
                onChange={(e) => {
                  setSubject(e.target.value);
                  changed();
                }}
              />
            </label>
            <label>
              {t.relatedKind}
              <select
                value={kind}
                onChange={(e) => {
                  setKind(e.target.value);
                  setRelated('');
                  setPrefill('');
                  setPage(1);
                  changed();
                }}
              >
                <option value="">{t.general}</option>
                <option value="requests">{t.request}</option>
                <option value="bookings">{t.booking}</option>
              </select>
            </label>
            {kind && (
              <section>
                <div className={styles.actions}>
                  <label>
                    {t.resourceSearch}
                    <input
                      maxLength={150}
                      value={search}
                      onChange={(e) => setSearch(e.target.value)}
                    />
                  </label>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => {
                      setQuery(search);
                      setPage(1);
                      setPrefill('');
                      setRelated('');
                      changed();
                    }}
                  >
                    {t.find}
                  </Button>
                </div>
                {links.error !== undefined ? (
                  <MessageError status={links.error} retry={links.refresh} />
                ) : !links.data ? (
                  <p>{t.loading}</p>
                ) : (
                  <>
                    <label>
                      {t.resource}
                      <select
                        required
                        value={related}
                        onChange={(e) => {
                          setRelated(e.target.value);
                          changed();
                        }}
                      >
                        <option value="">—</option>
                        {links.data.items.map((l) => (
                          <option key={l.id} value={l.id}>
                            {l.reference}
                          </option>
                        ))}
                      </select>
                    </label>
                    {!links.data.items.length && <p>{t.noLinks}</p>}
                    <CustomerPagination
                      page={links.data.page}
                      totalPages={links.data.totalPages}
                      totalCount={links.data.totalCount}
                      onPage={setPage}
                    />
                  </>
                )}
              </section>
            )}
            <label htmlFor={bodyId}>
              {t.message}
              <textarea
                id={bodyId}
                required
                rows={8}
                maxLength={max}
                value={body}
                onChange={(e) => {
                  setBody(e.target.value);
                  changed();
                }}
              />
            </label>
            <p>
              {max} {t.characters}
            </p>
            <Button
              type="submit"
              disabled={busy || !subject.trim() || !body.trim()}
            >
              {busy ? t.sending : t.send}
            </Button>
          </fieldset>
          {error !== undefined && <MessageError status={error} />}
        </form>
      )}
    </>
  );
}
