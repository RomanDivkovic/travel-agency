import type { ReactNode } from 'react';
import { useWizardCopy } from '../travel-request/copy';
import { useAdminCopy } from './copy';
import type { RequestDetail } from './model';
import styles from './Admin.module.scss';
function Rows({ rows }: { rows: [string, ReactNode][] }) {
  return (
    <dl>
      {rows.map(([label, value]) => (
        <div key={label}>
          <dt>{label}</dt>
          <dd>{value}</dd>
        </div>
      ))}
    </dl>
  );
}
export function RequestFields({ request: r }: { request: RequestDetail }) {
  const t = useAdminCopy();
  const w = useWizardCopy();
  const p = r.plan;
  const no = t.notProvided;
  const yes = (value: boolean) => (value ? w.yes : w.no);
  const labels: Record<string, string> = {
    priority: w.labels.priority,
    baggage: w.labels.baggage,
    flexibleAirports: w.labels.flexibleAirports,
    type: w.labels.accommodationType,
    breakfast: w.labels.breakfast,
    pool: w.labels.pool,
    familyFriendly: w.labels.familyFriendly,
    pace: w.labels.pace,
    interests: w.labels.interests,
  };
  const choices: Record<string, string> = w.choices;
  const rows: [string, ReactNode][] = [];
  try {
    const parsed: unknown = JSON.parse(p.preferencesJson);
    if (typeof parsed === 'object' && parsed !== null)
      for (const [group, items] of Object.entries(parsed))
        if (typeof items === 'object' && items !== null)
          for (const [key, value] of Object.entries(items)) {
            if (!labels[key]) continue;
            const translate = (v: unknown) =>
              typeof v === 'string'
                ? group === 'trip' && key === 'pace' && v === 'balanced'
                  ? w.choices.balancedPace
                  : (choices[v] ?? v)
                : no;
            rows.push([
              labels[key],
              typeof value === 'boolean'
                ? yes(value)
                : Array.isArray(value)
                  ? value.map(translate).join(', ') || no
                  : translate(value),
            ]);
          }
  } catch {
    /* A corrupt stored preference never becomes HTML. */
  }
  return (
    <div className={styles.detailGrid}>
      <section className={styles.panel}>
        <h2>{t.contact}</h2>
        <Rows
          rows={[
            [w.labels.name, r.contact.name],
            [
              w.labels.email,
              <a href={'mailto:' + encodeURIComponent(r.contact.email)}>
                {r.contact.email}
              </a>,
            ],
            [
              w.labels.phone,
              r.contact.phone ? (
                <a href={'tel:' + r.contact.phone.replace(/[^+\d]/g, '')}>
                  {r.contact.phone}
                </a>
              ) : (
                no
              ),
            ],
            [w.labels.contactMethod, w.choices[r.contact.preferredMethod]],
            [t.language, r.language === 'sv' ? 'Svenska' : 'English'],
          ]}
        />
      </section>
      <section className={styles.panel}>
        <h2>{t.travel}</h2>
        <Rows
          rows={[
            [w.labels.origin, p.origin ?? no],
            [w.labels.place, p.destination ?? no],
            [w.labels.flexibleDestination, yes(p.flexibleDestination)],
            [w.labels.stops, p.additionalStops.join(', ') || no],
            [
              w.labels.dateMode,
              p.dateMode === 'fixed' ? w.labels.fixed : w.labels.flexible,
            ],
            ...(p.dateMode === 'fixed'
              ? ([
                  [w.labels.departure, p.departure ?? no],
                  [w.labels.return, p.return ?? w.labels.oneWay],
                ] as [string, ReactNode][])
              : ([
                  [w.labels.earliest, p.earliest ?? no],
                  [w.labels.latest, p.latest ?? no],
                  [w.labels.duration, String(p.durationDays)],
                ] as [string, ReactNode][])),
            [w.labels.adults, p.adults],
            [w.labels.children, p.childrenAges.length],
            [w.labels.age, p.childrenAges.join(', ') || no],
            [w.labels.rooms, p.rooms ?? no],
          ]}
        />
      </section>
      <section className={styles.panel}>
        <h2>{t.budget}</h2>
        <Rows
          rows={[
            [
              w.labels.amount,
              `${p.budgetAmount.toLocaleString(r.language === 'sv' ? 'sv-SE' : 'en-GB')} ${p.currency}`,
            ],
            [w.labels.scope, w.choices[p.budgetScope]],
          ]}
        />
        <p>{t.noBudgetFlex}</p>
      </section>
      <section className={styles.panel}>
        <h2>{t.preferences}</h2>
        <Rows rows={rows} />
      </section>
      <section className={styles.panel}>
        <h2>{t.additional}</h2>
        <Rows rows={[[w.labels.activities, yes(p.activities)]]} />
        <p className={styles.prose}>{p.specialRequests ?? no}</p>
      </section>
    </div>
  );
}
