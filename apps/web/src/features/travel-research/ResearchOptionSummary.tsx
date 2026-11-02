import { useLanguage } from '../../localization/useLanguage';
import {
  flightMetrics,
  formatFlightTime,
  formatUtcTimestamp,
} from './formHelpers';
import { useResearchCopy } from './copy';
import { useMoney } from '../payments/useMoney';
import type { ResearchSnapshot } from './types';
import styles from './Research.module.scss';
export function ResearchOptionSummary({
  option: o,
}: {
  option: ResearchSnapshot;
}) {
  const t = useResearchCopy();
  const { language } = useLanguage();
  const money = useMoney();
  const f = o.flight;
  const a = o.accommodation;
  let supplierUrl: string | undefined;
  try {
    const url = new URL(o.supplierUrl);
    if (url.protocol === 'https:' && !url.username && !url.password)
      supplierUrl = url.href;
  } catch {
    /* Corrupt stored URLs never become executable links. */
  }
  return (
    <article className={styles.card}>
      <span className={styles.badge}>{t.kinds[o.kind]}</span>
      <h3>{o.title}</h3>
      {o.description && <p className={styles.prose}>{o.description}</p>}
      {f && (
        <>
          <dl className={styles.details}>
            <div>
              <dt>{t.duration}</dt>
              <dd>{flightMetrics(f.legs).durationHours.toFixed(2)}</dd>
            </div>
            <div>
              <dt>{t.stops}</dt>
              <dd>{flightMetrics(f.legs).connections}</dd>
            </div>
            <div>
              <dt>{t.cabin}</dt>
              <dd>{f.cabinClass}</dd>
            </div>
            <div>
              <dt>{t.baggage}</dt>
              <dd>{f.baggageDetails}</dd>
            </div>
          </dl>
          <ol>
            {f.legs.map((l, index) => (
              <li key={index}>
                <strong>
                  {l.airline} {l.flightNumber}
                </strong>
                <p>
                  {l.departureAirport} → {l.arrivalAirport}
                </p>
                <p>
                  {t.departureLabel}:{' '}
                  <time dateTime={l.departureAt}>
                    {formatFlightTime(
                      l.departureAt,
                      l.departureTimeZone,
                      language,
                    )}
                  </time>{' '}
                  ({l.departureTimeZone})
                </p>
                <p>
                  {t.arrivalLabel}:{' '}
                  <time dateTime={l.arrivalAt}>
                    {formatFlightTime(l.arrivalAt, l.arrivalTimeZone, language)}
                  </time>{' '}
                  ({l.arrivalTimeZone})
                </p>
              </li>
            ))}
          </ol>
        </>
      )}
      {a && (
        <dl className={styles.details}>
          {[
            [t.destination, a.destination],
            [t.address, a.address],
            [t.propertyType, a.accommodationType],
            [t.room, a.roomType],
            [t.checkIn, a.checkIn],
            [t.checkOut, a.checkOut],
            [t.guests, a.guests],
            [t.meal, a.mealPlan],
            [t.amenities, a.amenities],
          ].map(([label, value]) => (
            <div key={String(label)}>
              <dt>{label}</dt>
              <dd>{value}</dd>
            </div>
          ))}
        </dl>
      )}
      {o.extra && (
        <dl className={styles.details}>
          <div>
            <dt>{t.category}</dt>
            <dd>
              {t.categories[o.extra.category as keyof typeof t.categories] ??
                o.extra.category}
            </dd>
          </div>
          <div>
            <dt>{t.location}</dt>
            <dd>{o.extra.location}</dd>
          </div>
          {o.extra.date && (
            <div>
              <dt>{t.date}</dt>
              <dd>{o.extra.date}</dd>
            </div>
          )}
        </dl>
      )}
      <dl className={styles.details}>
        <div>
          <dt>{t.price}</dt>
          <dd>{money(o.priceMinorUnits, o.currency)}</dd>
        </div>
        <div>
          <dt>{t.basis}</dt>
          <dd>
            {t.bases[o.priceBasis as keyof typeof t.bases] ?? o.priceBasis}
          </dd>
        </div>
        <div>
          <dt>{t.quantity}</dt>
          <dd>{o.quantity}</dd>
        </div>
        <div className={styles.total}>
          <dt>{t.estimated}</dt>
          <dd>{money(o.totalMinorUnits, o.currency)}</dd>
        </div>
        <div>
          <dt>{t.supplier}</dt>
          <dd>
            <a href={supplierUrl} target="_blank" rel="noopener noreferrer">
              {o.supplierName}
            </a>
          </dd>
        </div>
        <div>
          <dt>{t.priceChecked}</dt>
          <dd>
            <time dateTime={o.priceCheckedAt}>
              {formatUtcTimestamp(o.priceCheckedAt, language)}
            </time>
          </dd>
        </div>
        <div>
          <dt>{t.availabilityChecked}</dt>
          <dd>
            {o.availabilityCheckedAt?.replace('T', ' ').replace('Z', ' UTC') ??
              t.notChecked}
          </dd>
        </div>
      </dl>
      <h4>{t.conditions}</h4>
      <p className={styles.prose}>{o.conditions}</p>
    </article>
  );
}
