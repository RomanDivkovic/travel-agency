import { apiClient, ApiError } from '../../services/apiClient';
import type {
  CustomerUser,
  Registration,
  Profile,
  ResourceKind,
  PortalItem,
  PageResult,
  Dashboard,
  RequestDetail,
  CustomerQuotation,
  Invoice,
  CustomerProposal,
  CustomerBooking,
  PortalActivity,
} from './types';
import type { PaymentSession } from '../payments/types';
export const customerExpiredEvent = 'travel-customer-session-ended';
function expired(error: unknown, path: string) {
  if (
    error instanceof ApiError &&
    error.status === 401 &&
    path !== '/auth/me' &&
    path !== '/auth/login'
  )
    window.dispatchEvent(new Event(customerExpiredEvent));
}
export async function customerRead<T>(
  path: string,
  signal?: AbortSignal,
): Promise<T> {
  try {
    return (
      await apiClient.get<T>('/api/customer' + path, {
        withCredentials: true,
        signal,
      })
    ).data;
  } catch (e) {
    expired(e, path);
    throw e;
  }
}
export async function customerMutation<T>(
  path: string,
  data?: unknown,
  method: 'post' | 'patch' = 'post',
): Promise<T> {
  try {
    const csrf = await customerRead<{ token: string }>('/auth/csrf');
    return (
      await apiClient.request<T>({
        url: '/api/customer' + path,
        method,
        data,
        withCredentials: true,
        headers: { 'X-CSRF-TOKEN': csrf.token },
      })
    ).data;
  } catch (e) {
    expired(e, path);
    throw e;
  }
}
const resource = (kind: ResourceKind) =>
  kind === 'requests' ? 'travel-requests' : kind;
const detailPath = (kind: ResourceKind, id: string) =>
  '/' + resource(kind) + '/' + encodeURIComponent(id);
export const customerAuthApi = {
  me: (signal?: AbortSignal) => customerRead<CustomerUser>('/auth/me', signal),
  login: (email: string, password: string) =>
    customerMutation<CustomerUser>('/auth/login', { email, password }),
  register: (data: Registration) =>
    customerMutation<void>('/auth/register', data),
  logout: () => customerMutation<void>('/auth/logout'),
  emailAction: (
    kind: 'forgot-password' | 'resend-verification',
    email: string,
  ) => customerMutation<void>('/auth/' + kind, { email }),
  verify: (token: string) =>
    customerMutation<void>('/auth/verify-email', { token }),
  reset: (token: string, password: string, passwordConfirmation: string) =>
    customerMutation<void>('/auth/reset-password', {
      token,
      password,
      passwordConfirmation,
    }),
  password: (
    currentPassword: string,
    password: string,
    passwordConfirmation: string,
  ) =>
    customerMutation<void>('/auth/change-password', {
      currentPassword,
      password,
      passwordConfirmation,
    }),
};
export const customerPortalApi = {
  dashboard: (s?: AbortSignal) => customerRead<Dashboard>('/dashboard', s),
  list: (kind: ResourceKind, query: URLSearchParams, s?: AbortSignal) =>
    customerRead<PageResult<PortalItem>>('/' + resource(kind) + '?' + query, s),
  request: (id: string, s?: AbortSignal) =>
    customerRead<RequestDetail>(detailPath('requests', id), s),
  quotation: (id: string, s?: AbortSignal) =>
    customerRead<CustomerQuotation>(detailPath('quotations', id), s),
  respondQuotation: (id: string, accept: boolean) =>
    customerMutation<CustomerQuotation>(
      detailPath('quotations', id) + (accept ? '/accept' : '/decline'),
      { confirmed: true },
    ),
  invoice: (id: string, s?: AbortSignal) =>
    customerRead<Invoice>(detailPath('invoices', id), s),
  payment: (id: string) =>
    customerMutation<PaymentSession>(
      detailPath('invoices', id) + '/payment-session',
    ),
  proposal: (id: string, s?: AbortSignal) =>
    customerRead<CustomerProposal>(detailPath('proposals', id), s),
  respondProposal: (
    id: string,
    p: CustomerProposal,
    approve: boolean,
    alternativeId: string | null,
    feedback: string,
  ) =>
    customerMutation<CustomerProposal>(
      detailPath('proposals', id) + (approve ? '/approve' : '/reject'),
      {
        version: p.version,
        confirmed: true,
        alternativeId,
        termsVersion: p.termsVersion,
        feedback,
      },
    ),
  booking: (id: string, s?: AbortSignal) =>
    customerRead<CustomerBooking>(detailPath('bookings', id), s),
  language: (preferredLanguage: 'sv' | 'en') =>
    customerMutation<CustomerUser>(
      '/profile/language',
      { preferredLanguage },
      'patch',
    ),
  profile: (input: Profile) =>
    customerMutation<CustomerUser>('/profile', input, 'patch'),
  claim: (reference: string) =>
    customerMutation<void>('/requests/claim', { reference }),
  confirmClaim: (token: string) =>
    customerMutation<void>('/requests/confirm-claim', { token }),
  activity: (requestId: string | undefined, page: number, s?: AbortSignal) =>
    customerRead<PageResult<PortalActivity>>(
      '/activity?' +
        new URLSearchParams({
          page: String(page),
          ...(requestId ? { requestId } : {}),
        }),
      s,
    ),
};
export async function downloadCustomerInvoice(id: string) {
  try {
    const r = await apiClient.get<Blob>(
      '/api/customer/invoices/' + encodeURIComponent(id) + '/document',
      { responseType: 'blob', withCredentials: true },
    );
    const url = URL.createObjectURL(r.data);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'invoice.html';
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  } catch (e) {
    expired(e, '/invoices');
    throw e;
  }
}
