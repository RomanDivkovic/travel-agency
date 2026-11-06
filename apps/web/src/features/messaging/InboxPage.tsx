import { useCallback } from 'react';
import { Link, useSearchParams } from 'react-router';
import { Button } from '../../components/ui/Button';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { CustomerPagination } from '../customer-portal/PortalShared';
import { messagingApi } from './api';
import { useMessagingCopy } from './copy';
import { useMessagingResource } from './useMessagingResource';
import { ConversationCards, MessageError } from './Shared';
import type { ConversationFilters } from './types';
import { ConversationFilters as FiltersForm } from './ConversationFilters';
import styles from './Messaging.module.scss';
export function InboxPage({ admin = false }: { admin?: boolean }) {
  const t = useMessagingCopy();
  usePageMetadata(admin ? t.inbox : t.messages);
  const [params, setParams] = useSearchParams();
  const filters: ConversationFilters = {
    page: Math.max(1, Number(params.get('page')) || 1),
    status: params.get('status') ?? '',
    priority: admin ? (params.get('priority') ?? '') : '',
    search: params.get('search') ?? '',
    unread: params.get('unread') === 'true',
    requestId: params.get('requestId') ?? '',
    bookingId: params.get('bookingId') ?? '',
  };
  const key = params.toString();
  const load = useCallback(
    (s: AbortSignal) =>
      messagingApi.list(
        admin,
        {
          page: Math.max(1, Number(params.get('page')) || 1),
          status: params.get('status') ?? '',
          priority: admin ? (params.get('priority') ?? '') : '',
          search: params.get('search') ?? '',
          unread: params.get('unread') === 'true',
          requestId: params.get('requestId') ?? '',
          bookingId: params.get('bookingId') ?? '',
        },
        s,
      ),
    [admin, params],
  );
  const state = useMessagingResource(key + '-' + admin, load);

  return (
    <>
      <h1>{admin ? t.inbox : t.messages}</h1>
      <div className={styles.actions}>
        {!admin && <Link to="/account/messages/new">{t.newConversation}</Link>}
        <Button variant="outline" onClick={state.refresh}>
          {t.refresh}
        </Button>
      </div>
      <p className={styles.notice}>{t.formal}</p>
      <FiltersForm
        key={key}
        initial={filters}
        admin={admin}
        onApply={(values) => {
          const p = new URLSearchParams(params);
          p.delete('page');
          for (const [name, value] of Object.entries(values)) {
            if (value !== '' && value !== false) p.set(name, String(value));
            else p.delete(name);
          }
          setParams(p);
        }}
      />
      {state.error !== undefined ? (
        <MessageError status={state.error} retry={state.refresh} />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          {state.data.items.length ? (
            <ConversationCards items={state.data.items} admin={admin} />
          ) : (
            <p>{t.empty}</p>
          )}
          <CustomerPagination
            page={state.data.page}
            totalPages={state.data.totalPages}
            totalCount={state.data.totalCount}
            onPage={(page) => {
              const p = new URLSearchParams(params);
              p.set('page', String(page));
              setParams(p);
            }}
          />
        </>
      )}
    </>
  );
}
