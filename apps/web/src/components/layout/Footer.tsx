import { Link } from 'react-router';
import { contactEmail } from '../../app/config/contact';
import { branding } from '../../app/config/branding';
import { routes } from '../../app/config/routes';
import { useTranslations } from '../../localization/useLanguage';
import { Container } from '../ui/Container';
import { BrandLogo } from './BrandLogo';
import styles from './Footer.module.scss';

export function Footer() {
  const t = useTranslations();
  return (
    <footer id="kontakt" tabIndex={-1} className={styles.footer}>
      <Container>
        <div className={styles.columns}>
          <div className={styles.brand}>
            <BrandLogo />
            <p>{t.footer.description}</p>
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
              {t.services.items.map((item) => (
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
            © {new Date().getFullYear()} {branding.name}
          </p>
          <div>
            <span>{t.footer.privacy}</span>
            <span>{t.footer.terms}</span>
          </div>
        </div>
      </Container>
    </footer>
  );
}
