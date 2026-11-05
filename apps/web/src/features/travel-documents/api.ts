import { apiClient, ApiError } from '../../services/apiClient';
import { adminRead, adminMutation, expiredEvent } from '../admin/api';
import { customerRead, customerExpiredEvent } from '../customer-portal/api';
import type {
  DocumentLibrary,
  DocumentMetadata,
  Itinerary,
  ItinerarySave,
} from './types';
const path = (id: string) => '/bookings/' + encodeURIComponent(id);
const read = <T>(admin: boolean, p: string, signal?: AbortSignal) =>
  admin ? adminRead<T>(p, signal) : customerRead<T>(p, signal);
export const documentApi = {
  library: (id: string, admin: boolean, page: number, signal?: AbortSignal) =>
    read<DocumentLibrary>(admin, path(id) + '/documents?page=' + page, signal),
  itinerary: (id: string, admin: boolean, signal?: AbortSignal) =>
    read<Itinerary>(admin, path(id) + '/itinerary', signal),
  async upload(
    id: string,
    file: File,
    metadata: DocumentMetadata,
    replace?: { id: string; versionToken: string },
  ) {
    const csrf = await adminRead<{ token: string }>('/auth/csrf');
    const data = new FormData();
    data.append('metadata', JSON.stringify(metadata));
    data.append('file', file);
    if (replace) data.append('versionToken', replace.versionToken);
    try {
      return (
        await apiClient.post<{ id: string }>(
          '/api/admin' +
            path(id) +
            '/documents' +
            (replace ? '/' + encodeURIComponent(replace.id) + '/replace' : ''),
          data,
          {
            withCredentials: true,
            timeout: 30000,
            headers: { 'X-CSRF-TOKEN': csrf.token },
          },
        )
      ).data;
    } catch (e) {
      if (e instanceof ApiError && e.status === 401)
        window.dispatchEvent(new Event(expiredEvent));
      throw e;
    }
  },
  edit: (
    booking: string,
    id: string,
    versionToken: string,
    name: string,
    description: string,
  ) =>
    adminMutation<void>(path(booking) + '/documents/' + id, 'patch', {
      versionToken,
      name,
      description,
    }),
  change: (
    booking: string,
    id: string,
    op: string,
    versionToken: string,
    version: number,
  ) =>
    adminMutation<void>(
      path(booking) + '/documents/' + id + '/' + op,
      'post',
      { versionToken, version },
      30000,
    ),
  itineraryAction: (booking: string, op: string, versionToken?: string) =>
    adminMutation<void>(
      path(booking) + '/itinerary' + (op === 'create' ? '' : '/' + op),
      'post',
      versionToken ? { versionToken } : undefined,
    ),
  save: (booking: string, input: ItinerarySave) =>
    adminMutation<void>(path(booking) + '/itinerary', 'patch', input),
  async binary(booking: string, admin: boolean, resource: string) {
    try {
      return (
        await apiClient.get<Blob>(
          (admin ? '/api/admin' : '/api/customer') +
            path(booking) +
            '/' +
            resource,
          { withCredentials: true, responseType: 'blob', timeout: 30000 },
        )
      ).data;
    } catch (e) {
      if (e instanceof ApiError && e.status === 401)
        window.dispatchEvent(
          new Event(admin ? expiredEvent : customerExpiredEvent),
        );
      throw e;
    }
  },
  file: (booking: string, id: string, admin: boolean, version?: number) =>
    documentApi.binary(
      booking,
      admin,
      'documents/' +
        encodeURIComponent(id) +
        '/download' +
        (admin && version ? '?version=' + version : ''),
    ),
  pdf: (booking: string, admin: boolean, language: string, version?: number) =>
    documentApi.binary(
      booking,
      admin,
      'itinerary/pdf?language=' +
        language +
        (admin && version ? '&version=' + version : ''),
    ),
};
export function saveBlob(blob: Blob, name: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = name;
  document.body.append(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
