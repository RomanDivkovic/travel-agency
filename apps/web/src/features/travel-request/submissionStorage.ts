import { draftKey } from './draft';
import {
  validReceipt,
  type SubmissionReceipt,
} from '../../services/travelRequests';
export const attemptKey = 'travel-agency.submission-attempt.v1';
export const receiptKey = 'travel-agency.submission-receipt.v1';
export function submissionAttempt(): string {
  const raw = localStorage.getItem(attemptKey);
  if (raw) {
    const value: unknown = JSON.parse(raw);
    if (!value || typeof value !== 'object')
      throw new Error('Recovery metadata unavailable');
    const { key, createdAt } = value as { key?: unknown; createdAt?: unknown };
    if (
      typeof key !== 'string' ||
      !/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(
        key,
      ) ||
      typeof createdAt !== 'number' ||
      !Number.isFinite(createdAt) ||
      Date.now() - createdAt > 7 * 86400000 ||
      createdAt > Date.now() + 300000
    )
      throw new Error('Recovery metadata unavailable');
    return key;
  }
  const key = crypto.randomUUID();
  localStorage.setItem(
    attemptKey,
    JSON.stringify({ key, createdAt: Date.now() }),
  );
  return key;
}
export function hasSubmissionAttempt() {
  try {
    return localStorage.getItem(attemptKey) !== null;
  } catch {
    return true;
  }
}
export function resetSubmissionAttempt() {
  try {
    localStorage.removeItem(attemptKey);
    return true;
  } catch {
    return false;
  }
}
export function confirmSubmission(receipt: SubmissionReceipt, key: string) {
  // Receipt contains no contact/travel data and is kept only in this browser tab.
  try {
    sessionStorage.setItem(
      receiptKey,
      JSON.stringify({ receipt, confirmedAt: Date.now() }),
    );
  } catch {
    /* Router context still carries this confirmed response. */
  }
  try {
    const stored = JSON.parse(localStorage.getItem(attemptKey) ?? 'null') as {
      key?: string;
    } | null;
    if (stored?.key === key) {
      localStorage.removeItem(draftKey);
      localStorage.removeItem(attemptKey);
    }
    return true;
  } catch {
    return false;
  }
}
export function storedReceipt(): SubmissionReceipt | undefined {
  try {
    const parsed: unknown = JSON.parse(
      sessionStorage.getItem(receiptKey) ?? 'null',
    );
    if (!parsed || typeof parsed !== 'object') return;
    const { receipt, confirmedAt } = parsed as {
      receipt?: unknown;
      confirmedAt?: unknown;
    };
    if (
      validReceipt(receipt) &&
      typeof confirmedAt === 'number' &&
      Date.now() - confirmedAt < 86400000 &&
      confirmedAt <= Date.now() + 300000
    )
      return receipt;
  } catch {
    /* A direct visit without confirmed context must never invent a receipt. */
  }
}
