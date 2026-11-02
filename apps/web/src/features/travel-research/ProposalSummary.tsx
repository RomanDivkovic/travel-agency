import { useLanguage } from '../../localization/useLanguage';
import { formatUtcTimestamp } from './formHelpers';
import { useResearchCopy } from './copy';
import { useMoney } from '../payments/useMoney';
import { ResearchOptionSummary } from './ResearchOptionSummary';
import { ResearchNotice } from './ResearchShared';
import type { CustomerProposal, EstimatedCost } from './types';
import styles from './Research.module.scss';
export function ProposalCosts({ costs }: { costs: EstimatedCost[] }) {
  const t = useResearchCopy();
  const money = useMoney();
  return (
    <div className={styles.grid}>
      {costs.map((c) => (
        <dl className={styles.details} key={c.currency}>
          <div>
            <dt>{t.flightCosts}</dt>
            <dd>{money(c.flightsMinorUnits, c.currency)}</dd>
          </div>
          <div>
            <dt>{t.stayCosts}</dt>
            <dd>{money(c.accommodationMinorUnits, c.currency)}</dd>
          </div>
          <div>
            <dt>{t.extraCosts}</dt>
            <dd>{money(c.extrasMinorUnits, c.currency)}</dd>
          </div>
          <div className={styles.total}>
            <dt>{t.supplierTotal}</dt>
            <dd>{money(c.totalMinorUnits, c.currency)}</dd>
          </div>
        </dl>
      ))}
    </div>
  );
}
export function ProposalSummary({
  proposal: p,
}: {
  proposal: CustomerProposal;
}) {
  const t = useResearchCopy();
  const money = useMoney();
  const trip = p.trip;
  const { language } = useLanguage();
  return (
    <>
      <section className={styles.panel}>
        <span className={styles.badge}>{t.statuses[p.status]}</span>
        <h2>{p.title}</h2>
        <p>
          {p.number} · v{p.version}
        </p>
        <p className={styles.prose}>{p.introduction}</p>
        <dl className={styles.details}>
          <div>
            <dt>{t.destination}</dt>
            <dd>
              {[trip.origin, trip.destination, ...trip.additionalStops]
                .filter(Boolean)
                .join(' → ') || '—'}
            </dd>
          </div>
          <div>
            <dt>{t.dates}</dt>
            <dd>
              {trip.dateMode === 'fixed'
                ? `${trip.departure ?? '—'} → ${trip.return ?? t.oneWay}`
                : `${t.flexible}: ${trip.earliest} — ${trip.latest} (${trip.durationDays} ${t.days})`}
            </dd>
          </div>
          <div>
            <dt>{t.travelers}</dt>
            <dd>
              {t.adults}: {trip.adults} · {t.children}: {trip.children}
              {trip.rooms ? ` · ${t.rooms}: ${trip.rooms}` : ''}
            </dd>
          </div>
          <div>
            <dt>{t.until}</dt>
            <dd>
              <time dateTime={p.validUntil}>
                {formatUtcTimestamp(p.validUntil, language)}
              </time>
            </dd>
          </div>
        </dl>
        <p className={styles.prose}>{p.summary}</p>
        <ResearchNotice />
      </section>
      <section className={styles.panel}>
        <h2>{t.paidFee}</h2>
        <p>
          {money(p.paidServiceFee.amountMinorUnits, p.paidServiceFee.currency)}{' '}
          · {p.paidServiceFee.invoiceNumber}
        </p>
        <p>{t.notDue}</p>
      </section>
      <section aria-label={t.alternatives}>
        <h2>{t.alternatives}</h2>
        {p.alternatives.map((a) => (
          <article key={a.id} className={styles.panel}>
            <h3>{a.title}</h3>
            {a.recommended && <p className={styles.badge}>{t.recommended}</p>}
            {p.selectedAlternativeId === a.id && (
              <p className={styles.notice}>{t.selection}</p>
            )}
            <p className={styles.prose}>{a.description}</p>
            <div className={styles.grid}>
              {a.items.map((i, n) => (
                <ResearchOptionSummary key={n} option={i} />
              ))}
            </div>
            <h4>{t.estimated}</h4>
            <ProposalCosts costs={a.estimatedCosts} />
          </article>
        ))}
      </section>
      <section className={styles.panel}>
        <h2>{t.terms}</h2>
        <p className={styles.prose}>{p.termsSnapshot || '—'}</p>
        <p>{p.termsVersion}</p>
        {p.acceptanceText && <p className={styles.prose}>{p.acceptanceText}</p>}
      </section>
    </>
  );
}
