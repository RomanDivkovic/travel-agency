import { adminRead, adminMutation } from '../admin/api';
import type { PageResult } from '../admin/model';
import type {
  Booking,
  BookingItem,
  BookingMetrics,
  BookingHistory,
  Evidence,
} from './types';
const path = (id: string) => '/bookings/' + encodeURIComponent(id);
export const bookingApi = {
  list: (query: URLSearchParams, signal?: AbortSignal) =>
    adminRead<PageResult<BookingItem>>('/bookings?' + query, signal),
  detail: (id: string, signal?: AbortSignal) =>
    adminRead<Booking>(path(id), signal),
  metrics: (signal?: AbortSignal) =>
    adminRead<BookingMetrics>('/bookings/metrics', signal),
  history: (id: string, page: number, signal?: AbortSignal) =>
    adminRead<PageResult<BookingHistory>>(
      `${path(id)}/activity?page=${page}`,
      signal,
    ),
  create: (id: string, authorization: Evidence) =>
    adminMutation<{ id: string }>(
      '/proposals/' + encodeURIComponent(id) + '/bookings',
      'post',
      { authorization },
    ),
  notes: (id: string, versionToken: string, notes: string) =>
    adminMutation<void>(path(id), 'patch', { versionToken, notes }),
  reservation: (
    id: string,
    rid: string,
    versionToken: string,
    action: string,
    input: object = {},
  ) =>
    adminMutation<void>(
      `${path(id)}/reservations/${encodeURIComponent(rid)}${action === 'update' ? '' : '/' + action}`,
      action === 'update' ? 'patch' : 'post',
      { versionToken, ...input },
    ),
  send: (id: string, versionToken: string) =>
    adminMutation<void>(path(id) + '/send-confirmation', 'post', {
      versionToken,
    }),
};
