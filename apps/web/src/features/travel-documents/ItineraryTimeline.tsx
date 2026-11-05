import { useLanguage } from '../../localization/useLanguage';
import { useDocumentCopy } from './copy';
import type { ItineraryVersion } from './types';
import styles from './Documents.module.scss';
import { travelDay, travelTime } from './time';
export function ItineraryTimeline({
  version: v,
}: {
  version: ItineraryVersion;
}) {
  const t = useDocumentCopy();
  const { language } = useLanguage();
  const days = new Map<string, typeof v.entries>();
  for (const e of v.entries) {
    const key = e.date ?? '';
    days.set(key, [...(days.get(key) ?? []), e]);
  }
  return (
    <section>
      <h2>{v.title}</h2>
      <p>{v.description}</p>
      <p>
        {v.trip.destination} · {v.trip.departure ?? v.trip.earliest} –{' '}
        {v.trip.return ?? v.trip.latest}
      </p>
      <p>
        {t.travelers}: {v.trip.adults + v.trip.children}
      </p>
      <p>{t.allDates}</p>
      {[...days.entries()].map(([day, entries]) => (
        <section key={day}>
          <h2 className={styles.subtitle}>
            {day ? travelDay(day, language) : t.unknownDate}
          </h2>
          <ol className={styles.timeline}>
            {entries.map((e) => (
              <li
                key={e.id}
                className={`${styles.entry} ${!e.confirmed ? styles.info : ''}`}
              >
                <p>
                  <strong>
                    {travelTime(e.at, e.timeZone, language) ?? t.unknownTime}
                  </strong>
                  {e.timeZone && <> · {e.timeZone}</>}
                </p>
                <h3>
                  {t.events[e.event]} · {e.title}
                </h3>
                <p>
                  <strong>{e.confirmed ? t.confirmed : t.information}</strong>
                </p>
                {e.location && <p>{e.location}</p>}
                {e.description && <p>{e.description}</p>}
                {e.supplierReference && (
                  <p>
                    {t.reference}: {e.supplierReference}
                  </p>
                )}
              </li>
            ))}
          </ol>
        </section>
      ))}
    </section>
  );
}
