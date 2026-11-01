import { Link } from 'react-router';
import { Button } from '../../components/ui/Button';
import { useDateFormat } from './useDateFormat';
import { useWizardCopy } from '../travel-request/copy';
import { useAdminCopy } from './copy';
import type { RequestItem, RequestStatus } from './model';
import styles from './Admin.module.scss';
export function StatusBadge({ status }: { status: RequestStatus }) {
  const t = useAdminCopy();
  return (
    <span className={`${styles.badge} ${styles[status]}`}>
      {t.statuses[status]}
    </span>
  );
}
export function ErrorState({
  status,
  retry,
}: {
  status: number;
  retry: () => void;
}) {
  const t = useAdminCopy();
  return (
    <div role="alert" className={styles.panel}>
      <p>
        {status === 403
          ? t.forbidden
          : status === 404
            ? t.notFound
            : status === 409
              ? t.conflict
              : status === 400
                ? t.invalid
                : t.error}
      </p>
      <Button onClick={retry}>{status === 409 ? t.refresh : t.retry}</Button>
    </div>
  );
}
export function RequestCards({
  items,
  nested = false,
}: {
  items: RequestItem[];
  nested?: boolean;
}) {
  const Heading = nested ? 'h3' : 'h2';
  const t = useAdminCopy();
  const w = useWizardCopy();
  const date = useDateFormat();
  return items.length ? (
    <div className={styles.requestGrid}>
      {items.map((r) => (
        <article className={styles.requestCard} key={r.id}>
          <div className={styles.cardTop}>
            <Link to={'/admin/requests/' + r.id}>{r.referenceNumber}</Link>
            <StatusBadge status={r.status} />
          </div>
          <Heading>{r.customerName}</Heading>
          <p>{w.serviceNames[r.serviceType]}</p>
          <dl>
            <dt>{t.destination}</dt>
            <dd>{r.destination ?? w.labels.flexibleDestination}</dd>
            <dt>{t.dates}</dt>
            <dd>
              {r.dateMode === 'flexible'
                ? `${w.labels.flexible}: ${r.earliest} – ${r.latest}`
                : `${r.departure} – ${r.return ?? w.labels.oneWay}`}
            </dd>
            <dt>{t.submitted}</dt>
            <dd>{date(r.createdAt)}</dd>
            <dt>{t.updated}</dt>
            <dd>{date(r.updatedAt)}</dd>
          </dl>
          <Link
            to={'/admin/requests/' + r.id}
            aria-label={`${t.open} ${r.referenceNumber}`}
          >
            {t.open} →
          </Link>
        </article>
      ))}
    </div>
  ) : (
    <p className={styles.panel}>{t.empty}</p>
  );
}
export function Pagination({
  page,
  totalPages,
  totalCount,
  onPage,
}: {
  page: number;
  totalPages: number;
  totalCount: number;
  onPage: (page: number) => void;
}) {
  const t = useAdminCopy();
  return (
    <nav className={styles.pagination} aria-label={t.page}>
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
        {t.next}
      </Button>
    </nav>
  );
}
