import { useMoney } from './useMoney';
import { useLanguage } from '../../localization/useLanguage';
import { usePaymentCopy } from './copy';
import type { Invoice, InvoiceItem } from './types';
import styles from './Payments.module.scss';
export function InvoiceSummary({ invoice: i }: { invoice: Invoice }) {
  const t = usePaymentCopy();
  const money = useMoney();
  const { language } = useLanguage();
  const date = (v: string) =>
    new Intl.DateTimeFormat(language === 'sv' ? 'sv-SE' : 'en-GB', {
      dateStyle: 'medium',
      timeZone: 'Europe/Stockholm',
    }).format(new Date(v));
  return (
    <section className={styles.panel} aria-label={t.detail}>
      {i.isSimulated && <p className={styles.notice}>{t.simulation}</p>}
      <span className={styles.badge}>{t.statuses[i.status]}</span>
      <h2>{i.number ?? t.draft}</h2>
      <p>{i.description}</p>
      <dl className={styles.details}>
        <div>
          <dt>{t.quote}</dt>
          <dd>
            {i.quotationNumber} v{i.quotationVersion}
          </dd>
        </div>
        <div>
          <dt>{t.customer}</dt>
          <dd>{i.customerName}</dd>
        </div>
        <div>
          <dt>{t.address}</dt>
          <dd className={styles.prose}>{i.customerAddress || '—'}</dd>
        </div>
        <div>
          <dt>{t.period}</dt>
          <dd>{i.servicePeriod || '—'}</dd>
        </div>
        <div>
          <dt>{t.due}</dt>
          <dd>{date(i.dueAt)}</dd>
        </div>
        {i.issuedAt && (
          <div>
            <dt>{t.issued}</dt>
            <dd>{date(i.issuedAt)}</dd>
          </div>
        )}
        {i.paidAt && (
          <div>
            <dt>{t.paidAt}</dt>
            <dd>{date(i.paidAt)}</dd>
          </div>
        )}
      </dl>
      <div className={styles.grid}>
        {i.lines.map((l, index) => (
          <article key={index} className={styles.line}>
            <h3>{l.description}</h3>
            <dl className={styles.details}>
              <div>
                <dt>{t.quantity}</dt>
                <dd>{l.quantity}</dd>
              </div>
              <div>
                <dt>{t.unit}</dt>
                <dd>{money(l.unitPriceMinorUnits, i.currency)}</dd>
              </div>
              <div>
                <dt>{t.rate}</dt>
                <dd>{l.taxRateBasisPoints / 100}%</dd>
              </div>
              <div>
                <dt>{t.tax}</dt>
                <dd>{money(l.taxMinorUnits, i.currency)}</dd>
              </div>
              <div>
                <dt>{t.total}</dt>
                <dd>{money(l.totalMinorUnits, i.currency)}</dd>
              </div>
            </dl>
          </article>
        ))}
      </div>
      <p>
        {t.taxTreatment}:{' '}
        {Object.hasOwn(t.treatments, i.taxTreatment)
          ? t.treatments[i.taxTreatment as keyof typeof t.treatments]
          : i.taxTreatment}
      </p>
      <dl className={styles.totals}>
        <div>
          <dt>{t.subtotal}</dt>
          <dd>{money(i.subtotalMinorUnits, i.currency)}</dd>
        </div>
        <div>
          <dt>{t.tax}</dt>
          <dd>{money(i.taxMinorUnits, i.currency)}</dd>
        </div>
        <div className={styles.grand}>
          <dt>{t.total}</dt>
          <dd>{money(i.totalMinorUnits, i.currency)}</dd>
        </div>
      </dl>
      {i.issuedAt && (
        <>
          <h3>{t.seller}</h3>
          <p className={styles.prose}>
            {i.sellerName}
            {'\n'}
            {i.sellerAddress}
            {'\n'}
            {i.registrationNumber}
            {'\n'}
            {i.taxInformation}
          </p>
          <h3>{t.terms}</h3>
          <p className={styles.prose}>{i.terms}</p>
          <p>{i.termsVersion}</p>
        </>
      )}
      <p>{t.noBooking}</p>
    </section>
  );
}
export function InvoiceItemSummary({ item: i }: { item: InvoiceItem }) {
  const t = usePaymentCopy();
  const money = useMoney();
  return (
    <>
      <strong>{i.number ?? t.draft}</strong>
      <p>{i.customerName}</p>
      <p>
        {money(i.totalMinorUnits, i.currency)} · {t.statuses[i.status]}
      </p>
      {i.isSimulated && <p>{t.simulation}</p>}
    </>
  );
}
