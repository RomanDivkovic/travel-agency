import { apiClient } from '../../services/apiClient';
import { adminRead, adminMutation } from '../admin/api';
import type {
  AdminInvoice,
  Invoice,
  InvoiceConfiguration,
  InvoicePage,
  PaymentSession,
  PaymentSummary,
  SandboxSession,
} from './types';
export const invoiceApi = {
  configuration: (signal?: AbortSignal) =>
    adminRead<InvoiceConfiguration>('/invoices/configuration', signal),
  forQuotation: (id: string, signal?: AbortSignal) =>
    adminRead<{ id: string | null }>(
      `/quotations/${encodeURIComponent(id)}/invoice`,
      signal,
    ),
  create: (id: string) =>
    adminMutation<{ id: string }>(
      `/quotations/${encodeURIComponent(id)}/invoice`,
      'post',
    ),
  list: (page: number, status: string, search: string, signal?: AbortSignal) =>
    adminRead<InvoicePage>(
      '/invoices?' +
        new URLSearchParams({ page: String(page), status, search }),
      signal,
    ),
  detail: (id: string, signal?: AbortSignal) =>
    adminRead<AdminInvoice>(`/invoices/${encodeURIComponent(id)}`, signal),
  save: (
    id: string,
    input: {
      versionToken: string;
      customerAddress: string;
      servicePeriod: string;
      dueAt: string;
    },
  ) =>
    adminMutation<void>(`/invoices/${encodeURIComponent(id)}`, 'patch', input),
  action: (
    id: string,
    action: 'issue' | 'cancel' | 'revoke-access',
    versionToken: string,
  ) =>
    adminMutation<void>(
      `/invoices/${encodeURIComponent(id)}/${action}`,
      'post',
      { versionToken },
    ),
  summary: (signal?: AbortSignal) =>
    adminRead<PaymentSummary>('/payments/summary', signal),
};
const headers = (token: string) => ({ 'X-Invoice-Token': token });
export const publicInvoiceApi = {
  review: async (token: string, signal?: AbortSignal) =>
    (
      await apiClient.get<Invoice>('/api/public/invoices', {
        headers: headers(token),
        withCredentials: false,
        signal,
      })
    ).data,
  session: async (token: string) =>
    (
      await apiClient.post<PaymentSession>(
        '/api/public/invoices/payment-session',
        {},
        { headers: headers(token), withCredentials: false },
      )
    ).data,
  sandbox: async (token: string, signal?: AbortSignal) =>
    (
      await apiClient.get<SandboxSession>('/api/public/payments/sandbox', {
        headers: { 'X-Sandbox-Token': token },
        withCredentials: false,
        signal,
      })
    ).data,
  simulate: async (token: string, outcome: string) => {
    await apiClient.post(
      '/api/public/payments/sandbox',
      { outcome },
      { headers: { 'X-Sandbox-Token': token }, withCredentials: false },
    );
  },
};
export async function downloadInvoice(token?: string, id?: string) {
  const response = await apiClient.get<Blob>(
    id
      ? `/api/admin/invoices/${encodeURIComponent(id)}/document`
      : '/api/public/invoices/document',
    {
      responseType: 'blob',
      withCredentials: Boolean(id),
      headers: token ? headers(token) : undefined,
    },
  );
  const url = URL.createObjectURL(response.data);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'invoice.html';
  a.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
export function safeCheckoutUrl(raw: string, simulated: boolean): string {
  const url = new URL(raw);
  if (
    simulated
      ? url.origin !== window.location.origin ||
        url.pathname !== '/payment/sandbox'
      : url.protocol !== 'https:'
  )
    throw new Error('Invalid checkout destination');
  return url.href;
}
