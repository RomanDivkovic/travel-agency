import { Link } from 'react-router';
import { useLanguage } from '../../localization/useLanguage';
import { useDateFormat } from '../admin/useDateFormat';
import { useWizardCopy } from '../travel-request/copy';
import { useQuotationCopy } from './copy';
import { formatMoney } from './money';
import { QuotationStatusBadge } from './QuotationSummary';
import type { QuotationListItem } from './types';
import styles from './Quotations.module.scss';
export function QuotationCards({
  items,
  nested = false,
}: {
  items: QuotationListItem[];
  nested?: boolean;
}) {
  const t = useQuotationCopy();
  const w = useWizardCopy();
  const { language } = useLanguage();
  const date = useDateFormat();
  const Heading = nested ? 'h3' : 'h2';
  return items.length ? (
    <div className={styles.cards}>
      {items.map((item) => (
        <article className={styles.panel} key={item.id}>
          <Heading>
            <Link to={'/admin/quotations/' + item.id}>
              {item.number} · {t.version} {item.version}
            </Link>
          </Heading>
          <QuotationStatusBadge status={item.status} />
          <p>
            {item.customerName} · {w.serviceNames[item.serviceType]}
          </p>
          <p>
            <Link to={'/admin/requests/' + item.requestId}>
              {item.requestReference}
            </Link>
          </p>
          <p>
            {t.total}:{' '}
            <strong>
              {formatMoney(item.totalMinorUnits, item.currency, language)}
            </strong>
          </p>
          <p>
            {t.created}: {date(item.createdAt)}
          </p>
          <p>
            {t.validUntil}: {date(item.validUntil)}
          </p>
          {item.sentAt ? (
            <p>
              {t.sentAt}: {date(item.sentAt)}
            </p>
          ) : null}
          {item.acceptedAt || item.declinedAt ? (
            <p>
              {t.response}: {date((item.acceptedAt ?? item.declinedAt)!)}
            </p>
          ) : null}
          <Link
            to={'/admin/quotations/' + item.id}
            aria-label={`${t.open} ${item.number} ${t.version} ${item.version}`}
          >
            {t.open} →
          </Link>
        </article>
      ))}
    </div>
  ) : (
    <p>{t.empty}</p>
  );
}
