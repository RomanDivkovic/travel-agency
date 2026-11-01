import { useLocation } from 'react-router';
import { Section } from '../../components/ui/Section';
import { ButtonLink } from '../../components/ui/Button';
import { routes } from '../../app/config/routes';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { validReceipt } from '../../services/travelRequests';
import { storedReceipt } from './submissionStorage';
import { useSubmissionCopy } from './submissionCopy';
import styles from './Wizard.module.scss';
export function SuccessPage() {
  const t = useSubmissionCopy();
  const location = useLocation();
  const state = location.state as {
    receipt?: unknown;
    cleanupFailed?: boolean;
  } | null;
  const receipt = validReceipt(state?.receipt)
    ? state.receipt
    : storedReceipt();
  usePageMetadata(
    receipt ? t.successTitle : t.missingTitle,
    receipt ? t.successMessage : t.missing,
  );
  return (
    <Section className={styles.page}>
      <div className={styles.intro}>
        <h1>{receipt ? t.successTitle : t.missingTitle}</h1>
        <p>{receipt ? t.successMessage : t.missing}</p>
        {receipt ? (
          <>
            <p className={styles.notice}>
              <strong>
                {t.reference}: {receipt.referenceNumber}
              </strong>
            </p>
            <p>{t.note}</p>
            <p>{t.emailNote}</p>
            {state?.cleanupFailed ? <p role="alert">{t.cleanup}</p> : null}
          </>
        ) : null}
        <div className={styles.actions}>
          <ButtonLink to={routes.home}>{t.home}</ButtonLink>
          {!receipt ? (
            <ButtonLink to={routes.planTrip} variant="outline">
              {t.wizard}
            </ButtonLink>
          ) : null}
        </div>
      </div>
    </Section>
  );
}
