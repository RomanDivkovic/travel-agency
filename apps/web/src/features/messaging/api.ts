import { adminRead, adminMutation } from '../admin/api';
import { customerRead, customerMutation } from '../customer-portal/api';
import type {
  ConversationDetail,
  ConversationFilters,
  ConversationItem,
  ConversationLink,
  MessageSummary,
  PageResult,
} from './types';
const base = '/conversations';
const path = (id: string) => base + '/' + encodeURIComponent(id);
const read = <T>(admin: boolean, url: string, signal?: AbortSignal) =>
  admin ? adminRead<T>(url, signal) : customerRead<T>(url, signal);
const mutate = <T>(
  admin: boolean,
  url: string,
  data?: unknown,
  method: 'post' | 'patch' = 'post',
) =>
  admin
    ? adminMutation<T>(url, method, data)
    : customerMutation<T>(url, data, method);
export const messagingApi = {
  list(admin: boolean, filters: ConversationFilters, signal?: AbortSignal) {
    const p = new URLSearchParams();
    for (const [key, v] of Object.entries(filters))
      if (v !== '' && v !== false) p.set(key, String(v));
    return read<PageResult<ConversationItem>>(admin, base + '?' + p, signal);
  },
  summary: (admin: boolean, signal?: AbortSignal) =>
    read<MessageSummary>(admin, base + '/unread-count', signal),
  detail: (admin: boolean, id: string, signal?: AbortSignal, before?: number) =>
    read<ConversationDetail>(
      admin,
      path(id) + (before ? '?before=' + before : ''),
      signal,
    ),
  configuration: (signal?: AbortSignal) =>
    customerRead<{ maxMessageLength: number }>(base + '/configuration', signal),
  links: (
    kind: string,
    page: number,
    search: string,
    id: string,
    signal?: AbortSignal,
  ) =>
    customerRead<PageResult<ConversationLink>>(
      base +
        '/links?' +
        new URLSearchParams({
          kind,
          page: String(page),
          search,
          ...(id ? { id } : {}),
        }),
      signal,
    ),
  create: (data: {
    clientId: string;
    subject: string;
    body: string;
    requestId: string | null;
    bookingId: string | null;
  }) => customerMutation<{ id: string }>(base, data),
  send: (admin: boolean, id: string, body: string, clientId: string) =>
    mutate<{ id: string }>(admin, path(id) + '/messages', { body, clientId }),
  read: (admin: boolean, id: string, throughSequence: number) =>
    mutate<void>(admin, path(id) + '/read', { throughSequence }),
  change: (
    id: string,
    kind: 'status' | 'priority',
    value: string,
    versionToken: string,
  ) =>
    mutate<void>(true, path(id) + '/' + kind, { value, versionToken }, 'patch'),
  preferences: (id: string, emailNotifications: boolean) =>
    customerMutation<void>(
      path(id) + '/notifications',
      { emailNotifications },
      'patch',
    ),
};
