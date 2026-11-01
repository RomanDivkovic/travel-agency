import { useLanguage } from '../../localization/useLanguage';
import { useDateFormat } from '../admin/useDateFormat';
import { formatMoney } from './money';
import { useQuotationCopy } from './copy';
import type { CustomerQuotation, QuotationStatus } from './types';
import styles from './Quotations.module.scss';
export function QuotationStatusBadge({ status }: { status: QuotationStatus }) {
  const t = useQuotationCopy();
  return <span className={styles.badge}>{t.statuses[status]}</span>;
}
export function QuotationSummary({
  quotation: q,
}: {
  quotation: CustomerQuotation;
}) {
  const t = useQuotationCopy();
  const { language } = useLanguage();
  const date = useDateFormat();
  const money = (n: number) => formatMoney(n, q.currency, language);
  return (
    <div className={styles.summary}>
      <section className={styles.panel}>
        <div className={styles.row}>
          <h2>
            {q.number} · {t.version} {q.version}
          </h2>
          <QuotationStatusBadge status={q.status} />
        </div>
        <p>{q.customerName}</p>
        <p className={styles.prose}>{q.description}</p>
        <p>
          {t.validUntil}:{' '}
          <time dateTime={q.validUntil}>{date(q.validUntil)}</time>
        </p>
        {q.status === 'Draft' ? <p>{t.draftHint}</p> : null}
        {q.status === 'Expired' ? <p role="status">{t.expired}</p> : null}
      </section>
      <div className={styles.twoColumns}>
        <section className={styles.panel}>
          <h2>{t.included}</h2>
          <p className={styles.prose}>{q.includedServices || '—'}</p>
        </section>
        <section className={styles.panel}>
          <h2>{t.excluded}</h2>
          <p className={styles.prose}>{q.excludedServices || '—'}</p>
        </section>
      </div>
      <section className={styles.panel}>
        <h2>{t.items}</h2>
        <p>
          {t.tax}: {t.taxModes[q.taxTreatment]}
        </p>
        {q.taxTreatment === 'Unconfirmed' ? <p>{t.unconfirmed}</p> : null}
        <div className={styles.lineCards}>
          {q.lines.map((line, i) => (
            <article className={styles.lineCard} key={i}>
              <h3>{line.description}</h3>
              <dl>
                <dt>{t.quantity}</dt>
                <dd>{line.quantity}</dd>
                <dt>{t.unitPrice}</dt>
                <dd>{money(line.unitPriceMinorUnits)}</dd>
                <dt>{t.taxRate}</dt>
                <dd>{line.taxRateBasisPoints / 100}%</dd>
                <dt>{t.total}</dt>
                <dd>{money(line.totalMinorUnits)}</dd>
              </dl>
            </article>
          ))}
        </div>
        <dl className={styles.totals}>
          <div>
            <dt>{t.subtotal}</dt>
            <dd>{money(q.subtotalMinorUnits)}</dd>
          </div>
          <div>
            <dt>{t.taxAmount}</dt>
            <dd>{money(q.taxMinorUnits)}</dd>
          </div>
          <div className={styles.grandTotal}>
            <dt>{t.total}</dt>
            <dd>{money(q.totalMinorUnits)}</dd>
          </div>
        </dl>
        <p>{t.supplierCosts}</p>
      </section>
      <section className={styles.panel}>
        <h2>{t.terms}</h2>
        {q.termsVersion ? (
          <p>
            {t.termsVersion}: {q.termsVersion}
          </p>
        ) : null}
        <p className={styles.prose}>{q.termsSnapshot || '—'}</p>
        <p>{t.noBooking}</p>
      </section>
      {q.customerNotes ? (
        <section className={styles.panel}>
          <h2>{t.notes}</h2>
          <p className={styles.prose}>{q.customerNotes}</p>
        </section>
      ) : null}
      {q.acceptedAt || q.declinedAt ? (
        <p>
          {t.response}: {date((q.acceptedAt ?? q.declinedAt)!)}
        </p>
      ) : null}
    </div>
  );
}
