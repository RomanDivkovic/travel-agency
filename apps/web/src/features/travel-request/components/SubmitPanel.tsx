import { useRef } from 'react';
import { Button } from '../../../components/ui/Button';
import type { TravelRequest } from '../model';
import type { FormErrors } from '../validation';
import { useRequestSubmission } from '../useRequestSubmission';
import { useSubmissionCopy } from '../submissionCopy';
import styles from '../Wizard.module.scss';
export function SubmitPanel({
  payload,
  onValidation,
  onBusy,
}: {
  payload: TravelRequest | undefined;
  onValidation: (errors: FormErrors) => void;
  onBusy: (busy: boolean) => void;
}) {
  const t = useSubmissionCopy();
  const website = useRef<HTMLInputElement>(null);
  const request = useRequestSubmission(onValidation, onBusy);
  return (
    <div className={styles.submission}>
      <p>{t.intro}</p>
      <div hidden aria-hidden="true">
        <label htmlFor="request-website">Website</label>
        <input
          id="request-website"
          name="website"
          autoComplete="off"
          tabIndex={-1}
          ref={website}
        />
      </div>
      {request.state !== 'idle' && request.state !== 'submitting' ? (
        <p role="alert">
          {t[request.state]}
          {request.state === 'rate' ? ` ${request.remaining} ${t.seconds}` : ''}
        </p>
      ) : null}
      <Button
        disabled={!payload || request.busy || request.remaining > 0}
        onClick={() => {
          if (payload)
            void request.submit(payload, website.current?.value ?? '');
        }}
      >
        {request.busy
          ? t.submitting
          : request.state === 'idle' || request.state === 'validation'
            ? t.submit
            : t.retry}
      </Button>
      {request.busy ? <p role="status">{t.submitting}</p> : null}
    </div>
  );
}
