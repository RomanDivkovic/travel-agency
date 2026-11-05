import { useId, type InputHTMLAttributes, type ReactNode } from 'react';
import { Link } from 'react-router';
import { Button } from '../../components/ui/Button';
import { useMoney } from '../payments/useMoney';
import { useDateFormat } from '../admin/useDateFormat';
import { useCustomerCopy } from './copy';
import type { PortalItem, Journey, PortalActivity } from './types';
import styles from './Customer.module.scss';
export function Field({
  label,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & { label: string }) {
  const id = useId();
  return (
    <label htmlFor={id}>
      {label}
      <input {...props} id={id} />
    </label>
  );
}
export function CustomerError({
  status,
  retry,
}: {
  status: number;
  retry?: () => void;
}) {
  const t = useCustomerCopy();
  return (
    <div role="alert">
      <p>
        {status === 400
          ? t.invalid
          : status === 401
            ? t.credentials
            : status === 403
              ? t.forbidden
              : status === 404
                ? t.unavailable
                : status === 409
                  ? t.conflict
                  : status === 412
                    ? t.emailUnavailable
                    : status === 429
                      ? t.limited
                      : t.error}
      </p>
      {retry && (
        <Button variant="outline" onClick={retry}>
          {t.retry}
        </Button>
      )}
    </div>
  );
}
export function PortalLoad({
  error,
  loading,
  retry,
  children,
}: {
  error?: number;
  loading: boolean;
  retry: () => void;
  children: ReactNode;
}) {
  const t = useCustomerCopy();
  return error !== undefined ? (
    <CustomerError status={error} retry={retry} />
  ) : loading ? (
    <p role="status">{t.loading}</p>
  ) : (
    <>{children}</>
  );
}
const journeySteps = [
  'InquirySubmitted',
  'UnderReview',
  'QuotationSent',
  'QuotationAccepted',
  'PaymentConfirmed',
  'TravelResearch',
  'ProposalSent',
  'ProposalApproved',
  'BookingInProgress',
  'BookingConfirmed',
];
export function JourneyProgress({ journey: j }: { journey: Journey }) {
  const t = useCustomerCopy();
  return (
    <section className={styles.panel}>
      <h2>{t.journey}</h2>
      <p role="status">
        <strong>
          {t.current}: {t.stages[j.stage] ?? t.status}
        </strong>
      </p>
      <ol className={styles.progress}>
        {journeySteps.map((step) => (
          <li key={step} className={j.stage === step ? styles.current : ''}>
            <span aria-hidden="true">
              {j.completedStages.includes(step) ? '✓' : '○'}{' '}
            </span>
            {t.stages[step]}
            {j.completedStages.includes(step) && <span> · {t.completed}</span>}
          </li>
        ))}
      </ol>
      {j.nextAction && (
        <p>
          {t.next}: {t.actionLabels[j.nextAction]}
        </p>
      )}
      {j.nextAction === 'ContactAgent' && (
        <Link to="/contact">{t.actionLabels.ContactAgent}</Link>
      )}
    </section>
  );
}
export function PortalCards({ items }: { items: PortalItem[] }) {
  const t = useCustomerCopy();
  const money = useMoney(),
    date = useDateFormat();
  return items.length ? (
    <div className={styles.grid}>
      {items.map((item) => (
        <article className={styles.card} key={item.kind + item.id}>
          <h3>
            <Link to={'/account/' + item.kind + '/' + item.id}>
              {item.number}
            </Link>
          </h3>
          <p>
            {item.kind === 'requests' ? t.services[item.title] : item.title}
          </p>
          <p>
            {item.destination ?? t.destination}
            {item.departure ? ' · ' + item.departure : ''}
          </p>
          <p className={styles.badge}>{t.statuses[item.status] ?? t.status}</p>
          {item.isSimulated && <p className={styles.notice}>{t.simulated}</p>}
          {item.amountMinorUnits !== null && item.currency && (
            <p>
              {t.serviceFee}: {money(item.amountMinorUnits, item.currency)}
            </p>
          )}
          {item.validUntil && (
            <p>
              {t.validUntil}: {date(item.validUntil)}
            </p>
          )}
          {item.journey && (
            <p>
              {t.current}: {t.stages[item.journey.stage]}
            </p>
          )}
          {item.journey?.nextAction && (
            <p>
              {t.next}: {t.actionLabels[item.journey.nextAction]}
            </p>
          )}
        </article>
      ))}
    </div>
  ) : (
    <p>{t.empty}</p>
  );
}
export function ActivityList({ items }: { items: PortalActivity[] }) {
  const t = useCustomerCopy();
  const date = useDateFormat();
  return items.length ? (
    <ol>
      {items.map((e) => (
        <li key={e.id}>
          <p>
            {t.events[e.type] ?? t.activity} · {date(e.createdAt)}
          </p>
        </li>
      ))}
    </ol>
  ) : (
    <p>{t.empty}</p>
  );
}
export function CustomerPagination({
  page,
  totalPages,
  totalCount,
  onPage,
}: {
  page: number;
  totalPages: number;
  totalCount: number;
  onPage: (p: number) => void;
}) {
  const t = useCustomerCopy();
  return (
    <nav aria-label={t.page} className={styles.actions}>
      <Button
        variant="outline"
        disabled={page <= 1}
        onClick={() => onPage(page - 1)}
      >
        {t.previous}
      </Button>
      <span aria-live="polite">
        {t.page} {page} {t.of} {Math.max(1, totalPages)} · {totalCount}{' '}
        {t.results}
      </span>
      <Button
        variant="outline"
        disabled={page >= totalPages}
        onClick={() => onPage(page + 1)}
      >
        {t.nextPage}
      </Button>
    </nav>
  );
}
