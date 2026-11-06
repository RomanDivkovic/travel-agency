import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useContext } from 'react';
import { useParams, Link } from 'react-router';
import { useLanguage } from '../../localization/useLanguage';
import { useContent, useWebsite, PreviewContext } from './contentContext';
import { mediaUrl } from './api';
import type { BusinessData, PageData, ServiceData, PolicyData } from './types';
import { Section } from '../../components/ui/Section';
export function WebsiteImage({
  page,
  admin = false,
  className,
}: {
  page: string;
  admin?: boolean;
  className?: string;
}) {
  const preview = useContext(PreviewContext);
  const data = useContent<PageData>('page:' + page);
  const { language } = useLanguage();
  if (!data?.image) return null;
  return (
    <img
      className={className}
      src={mediaUrl(data.image.id, admin || preview)}
      alt={
        data.image.decorative
          ? ''
          : language === 'sv'
            ? data.image.altSv
            : data.image.altEn
      }
      style={{ maxWidth: '100%', height: 'auto', borderRadius: 12 }}
    />
  );
}
export function ServiceFee({ service }: { service?: ServiceData }) {
  const { language } = useLanguage();
  const p = service?.pricing;
  const now = new Date();
  if (
    !p ||
    p.type === 'CustomQuotation' ||
    p.amountMinor === null ||
    (p.effectiveFrom && new Date(p.effectiveFrom) > now) ||
    (p.effectiveUntil && new Date(p.effectiveUntil) <= now)
  )
    return <>{language === 'sv' ? 'Personlig offert' : 'Personal quotation'}</>;
  return (
    <>
      {p.type === 'StartingFrom' ? (language === 'sv' ? 'Från ' : 'From ') : ''}
      {new Intl.NumberFormat(language, {
        style: 'currency',
        currency: p.currency,
      }).format(p.amountMinor / 100)}
    </>
  );
}
export function BusinessContact() {
  const b = useContent<BusinessData>('business');
  const { language } = useLanguage();
  if (!b) return null;
  const sv = language === 'sv';
  return (
    <>
      {b.legalBusinessName && <p>{b.legalBusinessName}</p>}
      {b.organizationNumber && (
        <p>
          {sv ? 'Organisationsnummer' : 'Organization number'}:{' '}
          {b.organizationNumber}
        </p>
      )}
      {b.websiteUrl && (
        <p>
          <a href={b.websiteUrl} rel="noreferrer">
            {b.websiteUrl}
          </a>
        </p>
      )}
      {b.contactPhone && (
        <p>
          <a href={'tel:' + b.contactPhone.replace(/[^+0-9]/g, '')}>
            {b.contactPhone}
          </a>
        </p>
      )}
      {b.address && (
        <p>
          {b.address}
          <br />
          {b.postalCode} {b.city}
        </p>
      )}
      {b.openingHours.length > 0 && (
        <div>
          <h3>
            {sv ? 'Öppettider' : 'Opening hours'} — {b.timeZone}
          </h3>
          <ul>
            {[1, 2, 3, 4, 5, 6, 0].map((day) => (
              <li key={day}>
                {new Intl.DateTimeFormat(language, {
                  weekday: 'long',
                  timeZone: 'UTC',
                }).format(new Date(Date.UTC(2026, 10, 1 + day)))}
                :{' '}
                {b.openingHours
                  .filter((x) => x.day === day)
                  .map((x) => x.opens + '–' + x.closes)
                  .join(', ') || (sv ? 'Stängt' : 'Closed')}
              </li>
            ))}
          </ul>
        </div>
      )}
      {b.exceptions.length > 0 && (
        <div>
          <h3>{sv ? 'Avvikande öppettider' : 'Special opening hours'}</h3>
          <ul>
            {b.exceptions.map((x) => (
              <li key={x.date}>
                {x.date}:{' '}
                {x.closed
                  ? sv
                    ? 'Stängt'
                    : 'Closed'
                  : x.opens + '–' + x.closes}
              </li>
            ))}
          </ul>
        </div>
      )}
    </>
  );
}
export function PolicyLinks() {
  const { documents } = useWebsite();
  const { language } = useLanguage();
  return (
    <>
      {Object.entries(documents)
        .filter(([k]) => k.startsWith('policy:'))
        .map(([k, d]) => (
          <Link key={k} to={'/policies/' + k.slice(7)}>
            {(d as PolicyData)[language].title}
          </Link>
        ))}
    </>
  );
}
export function PolicyPage() {
  const { type = '' } = useParams();
  const d = useContent<PolicyData>('policy:' + type);
  const { language } = useLanguage();
  usePageMetadata(
    d ? d[language].title : language === 'sv' ? 'Policy' : 'Policy',
  );
  return (
    <Section>
      <h1>
        {d
          ? d[language].title
          : language === 'sv'
            ? 'Policyn är inte publicerad'
            : 'Policy not published'}
      </h1>
      {d && (
        <>
          <p>
            {language === 'sv' ? 'Gäller från' : 'Effective from'}:{' '}
            {d.effectiveFrom}
          </p>
          <div style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>
            {d[language].content}
          </div>
        </>
      )}
    </Section>
  );
}

export function ManagedServiceDetails({ code }: { code: string }) {
  const d = useContent<ServiceData>('service:' + code);
  const { language } = useLanguage();
  if (!d) return null;
  return (
    <>
      {d[language].fullDescription !== d[language].shortDescription && (
        <p>{d[language].fullDescription}</p>
      )}
      <h3>{language === 'sv' ? 'Ingår inte' : 'Not included'}</h3>
      <p>{d[language].excluded}</p>
    </>
  );
}
