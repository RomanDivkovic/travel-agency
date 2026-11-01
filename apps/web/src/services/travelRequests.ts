import { apiClient } from './apiClient';
import type { TravelRequest } from '../features/travel-request/model';
export interface SubmissionReceipt {
  referenceNumber: string;
  status: 'submitted';
  submittedAt: string;
}
export function validReceipt(value: unknown): value is SubmissionReceipt {
  if (!value || typeof value !== 'object') return false;
  const r = value as Partial<SubmissionReceipt>;
  return (
    typeof r.referenceNumber === 'string' &&
    /^[A-Z]{2,8}-\d{4}-\d{6,19}$/.test(r.referenceNumber) &&
    r.status === 'submitted' &&
    typeof r.submittedAt === 'string' &&
    Number.isFinite(Date.parse(r.submittedAt))
  );
}
export async function submitTravelRequest(
  payload: TravelRequest,
  key: string,
  website = '',
): Promise<SubmissionReceipt> {
  const response = await apiClient.post<unknown>(
    '/api/travel-requests',
    { ...payload, website },
    { headers: { 'Idempotency-Key': key } },
  );
  if (response.status !== 201 || !validReceipt(response.data))
    throw new Error('Submission receipt could not be confirmed.');
  return response.data;
}
