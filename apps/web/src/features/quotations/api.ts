import { adminRead, adminMutation } from '../admin/api';
import { apiClient } from '../../services/apiClient';
import type {
  AdminQuotation,
  CustomerQuotation,
  DraftInput,
  QuotationConfiguration,
  QuotationFilters,
  QuotationHistory,
  QuotationPage,
} from './types';
export const quotationApi = {
  configuration: (signal?: AbortSignal) =>
    adminRead<QuotationConfiguration>('/quotations/configuration', signal),
  list: (filters: QuotationFilters, signal?: AbortSignal) => {
    const q = new URLSearchParams();
    for (const [k, v] of Object.entries(filters))
      if (v !== '') q.set(k, String(v));
    return adminRead<QuotationPage>('/quotations?' + q, signal);
  },
  forRequest: (id: string, page = 1, signal?: AbortSignal) =>
    adminRead<QuotationPage>(
      `/travel-requests/${encodeURIComponent(id)}/quotations?page=${page}`,
      signal,
    ),
  create: (requestId: string) =>
    adminMutation<{ id: string }>(
      `/travel-requests/${encodeURIComponent(requestId)}/quotations`,
      'post',
    ),
  detail: (id: string, signal?: AbortSignal) =>
    adminRead<AdminQuotation>('/quotations/' + encodeURIComponent(id), signal),
  save: (id: string, input: DraftInput) =>
    adminMutation<void>(
      '/quotations/' + encodeURIComponent(id),
      'patch',
      input,
    ),
  send: (id: string, versionToken: string) =>
    adminMutation<void>(`/quotations/${encodeURIComponent(id)}/send`, 'post', {
      versionToken,
    }),
  revise: (id: string, versionToken: string) =>
    adminMutation<{ id: string }>(
      `/quotations/${encodeURIComponent(id)}/revise`,
      'post',
      { versionToken },
    ),
  cancel: (id: string, versionToken: string) =>
    adminMutation<void>(
      `/quotations/${encodeURIComponent(id)}/cancel`,
      'post',
      { versionToken },
    ),
  history: (id: string, page: number, signal?: AbortSignal) =>
    adminRead<QuotationHistory>(
      `/quotations/${encodeURIComponent(id)}/activity?page=${page}`,
      signal,
    ),
};
export const publicQuotationApi = {
  review: async (token: string, signal?: AbortSignal) =>
    (
      await apiClient.get<CustomerQuotation>('/api/public/quotations', {
        headers: { 'X-Quotation-Token': token },
        withCredentials: false,
        signal,
      })
    ).data,
  respond: async (token: string, accept: boolean) =>
    (
      await apiClient.post<CustomerQuotation>(
        `/api/public/quotations/${accept ? 'accept' : 'decline'}`,
        { confirmed: true },
        { headers: { 'X-Quotation-Token': token }, withCredentials: false },
      )
    ).data,
};
