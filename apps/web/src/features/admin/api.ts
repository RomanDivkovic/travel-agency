import { apiClient, ApiError } from '../../services/apiClient';
import type {
  Activity,
  AdminUser,
  Dashboard,
  Filters,
  Note,
  PageResult,
  RequestDetail,
  RequestItem,
  RequestStatus,
} from './model';
export const expiredEvent = 'travel-admin-session-ended';
async function read<T>(path: string, signal?: AbortSignal): Promise<T> {
  try {
    return (
      await apiClient.get<T>('/api/admin' + path, {
        withCredentials: true,
        signal,
      })
    ).data;
  } catch (error) {
    if (
      error instanceof ApiError &&
      error.status === 401 &&
      path !== '/auth/me'
    )
      window.dispatchEvent(new Event(expiredEvent));
    throw error;
  }
}
async function mutate<T>(
  path: string,
  method: 'post' | 'patch',
  data?: unknown,
): Promise<T> {
  const token = await read<{ token: string }>('/auth/csrf');
  try {
    return (
      await apiClient.request<T>({
        url: '/api/admin' + path,
        method,
        data,
        withCredentials: true,
        headers: { 'X-CSRF-TOKEN': token.token },
      })
    ).data;
  } catch (error) {
    if (
      error instanceof ApiError &&
      error.status === 401 &&
      path !== '/auth/login'
    )
      window.dispatchEvent(new Event(expiredEvent));
    throw error;
  }
}
export const adminApi = {
  me: (signal?: AbortSignal) => read<AdminUser>('/auth/me', signal),
  login: (email: string, password: string) =>
    mutate<AdminUser>('/auth/login', 'post', { email, password }),
  logout: () => mutate<void>('/auth/logout', 'post'),
  dashboard: (signal?: AbortSignal) =>
    read<Dashboard>('/dashboard/summary', signal),
  list: (filters: Filters, signal?: AbortSignal) => {
    const q = new URLSearchParams();
    for (const [k, v] of Object.entries(filters))
      if (v !== '') q.set(k, String(v));
    return read<PageResult<RequestItem>>(
      '/travel-requests?' + q.toString(),
      signal,
    );
  },
  detail: (id: string, signal?: AbortSignal) =>
    read<RequestDetail>('/travel-requests/' + encodeURIComponent(id), signal),
  notes: (id: string, page: number, signal?: AbortSignal) =>
    read<PageResult<Note>>(
      `/travel-requests/${encodeURIComponent(id)}/notes?page=${page}`,
      signal,
    ),
  activity: (id: string, page: number, signal?: AbortSignal) =>
    read<PageResult<Activity>>(
      `/travel-requests/${encodeURIComponent(id)}/activity?page=${page}`,
      signal,
    ),
  status: (id: string, status: RequestStatus, version: string) =>
    mutate<void>(`/travel-requests/${encodeURIComponent(id)}/status`, 'patch', {
      status,
      version,
    }),
  addNote: (id: string, content: string) =>
    mutate<void>(`/travel-requests/${encodeURIComponent(id)}/notes`, 'post', {
      content,
    }),
};
