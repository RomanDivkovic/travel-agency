import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router';
import { ApiError } from '../../services/apiClient';
import { submitTravelRequest } from '../../services/travelRequests';
import { confirmSubmission, submissionAttempt } from './submissionStorage';
import type { TravelRequest } from './model';
import type { FormErrors, ErrorCode } from './validation';
export type SubmissionState =
  | 'idle'
  | 'submitting'
  | 'validation'
  | 'network'
  | 'server'
  | 'rate'
  | 'conflict'
  | 'storage';
const codes: ErrorCode[] = [
  'required',
  'length',
  'date',
  'dateOrder',
  'duration',
  'adults',
  'children',
  'age',
  'rooms',
  'budget',
  'email',
  'phone',
  'choice',
];
export function validationFields(details: unknown): FormErrors {
  if (
    !details ||
    typeof details !== 'object' ||
    !('errors' in details) ||
    !details.errors ||
    typeof details.errors !== 'object'
  )
    return {};
  return Object.fromEntries(
    Object.entries(details.errors).flatMap(([key, value]) =>
      Array.isArray(value) && codes.includes(value[0] as ErrorCode)
        ? [[key, value[0] as ErrorCode]]
        : [],
    ),
  );
}
export function retryDeadline(value: string | undefined) {
  const seconds = Number(value);
  const delay =
    value && Number.isFinite(seconds)
      ? Math.max(1, seconds) * 1000
      : value
        ? Date.parse(value) - Date.now()
        : 60000;
  return (
    Date.now() +
    Math.max(1000, Math.min(86400000, Number.isFinite(delay) ? delay : 60000))
  );
}
export function useRequestSubmission(
  onValidation: (errors: FormErrors) => void,
  onBusy: (value: boolean) => void,
) {
  const [state, setState] = useState<SubmissionState>('idle');
  const [retryAt, setRetryAt] = useState(0);
  const [now, setNow] = useState(Date.now);
  const inflight = useRef(false);
  const mounted = useRef(true);
  const navigate = useNavigate();
  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
    };
  }, []);
  useEffect(() => {
    if (state !== 'rate') return;
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, [state]);
  async function submit(payload: TravelRequest, website: string) {
    if (inflight.current || Date.now() < retryAt) return;
    let key: string;
    try {
      key = submissionAttempt();
    } catch {
      setState('storage');
      return;
    }
    inflight.current = true;
    setState('submitting');
    onBusy(true);
    try {
      const receipt = await submitTravelRequest(payload, key, website);
      const cleaned = confirmSubmission(receipt, key);
      if (mounted.current)
        navigate('/plan-your-trip/success', {
          replace: true,
          state: { receipt, cleanupFailed: !cleaned },
        });
    } catch (error) {
      if (!mounted.current) return;
      if (error instanceof ApiError && error.status === 400) {
        setState('validation');
        onValidation(validationFields(error.details));
      } else if (error instanceof ApiError && error.status === 409)
        setState('conflict');
      else if (error instanceof ApiError && error.status === 429) {
        const deadline = retryDeadline(error.retryAfter);
        setRetryAt(deadline);
        setNow(Date.now());
        setState('rate');
      } else if (error instanceof ApiError && error.status === undefined)
        setState('network');
      else setState('server');
    } finally {
      inflight.current = false;
      onBusy(false);
    }
  }
  return {
    state,
    submit,
    remaining: Math.max(0, Math.ceil((retryAt - now) / 1000)),
    busy: state === 'submitting',
  };
}
