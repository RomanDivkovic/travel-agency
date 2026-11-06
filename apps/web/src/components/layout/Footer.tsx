import {
  useWebsite,
  isServiceEnabled,
} from '../../features/business-management/contentContext';
import {
  serviceCodes,
  cardIds,
} from '../../features/business-management/types';
import { useContent } from '../../features/business-management/contentContext';
import type { BusinessData } from '../../features/business-management/types';
import {
  PolicyLinks,
  WebsiteImage,
} from '../../features/business-management/PublishedComponents';
import { Link } from 'react-router';
import { contactEmail as fallbackEmail } from '../../app/config/contact';
import { branding } from '../../app/config/branding';
import { routes } from '../../app/config/routes';
import { useTranslations } from '../../localization/useLanguage';
import { Container } from '../ui/Container';
import { BrandLogo } from './BrandLogo';
import styles from './Footer.module.scss';

export function Footer() {
  const t = useTranslations();
  const { documents } = useWebsite();
  const business = useContent<BusinessData>('business');
  const contactEmail = business
    ? business.publicContactVisible
      ? business.contactEmail
      : ''
    : fallbackEmail;
  return (
    <footer id="kontakt" tabIndex={-1} className={styles.footer}>
      <Container>
        <div className={styles.columns}>
          <div className={styles.brand}>
            <BrandLogo />
            <p>{t.footer.description}</p>
            <WebsiteImage page="footer" />
          </div>
          <div>
            <h2>{t.footer.explore}</h2>
            <nav aria-label={t.footer.explore}>
              {t.navigation.items.map((item) => (
                <Link key={item.destination} to={routes[item.destination]}>
                  {item.label}
                </Link>
              ))}
              <Link to={routes.process}>{t.process.title}</Link>
            </nav>
          </div>
          <div>
            <h2>{t.footer.services}</h2>
            <nav aria-label={t.footer.services}>
              {t.services.items
                .filter((s) =>
                  isServiceEnabled(
                    documents,
                    serviceCodes[cardIds.indexOf(s.id)] || '',
                  ),
                )
                .map((item) => (
                  <Link key={item.id} to={`${routes.services}#${item.id}`}>
                    {item.title}
                  </Link>
                ))}
            </nav>
          </div>
          <div>
            <h2>{t.footer.contact}</h2>
            <p>
              {contactEmail ? (
                <a href={`mailto:${contactEmail}`}>{contactEmail}</a>
              ) : (
                t.footer.contactPlaceholder
              )}
            </p>
          </div>
        </div>
        <div className={styles.bottom}>
          <p>
            © {new Date().getFullYear()}{' '}
            {business?.businessName || branding.name}
          </p>
          <div>
            <PolicyLinks />
            {!documents['policy:Privacy'] && <span>{t.footer.privacy}</span>}
            {!documents['policy:Terms'] && <span>{t.footer.terms}</span>}
          </div>
        </div>
      </Container>
    </footer>
  );
}
