import { Link } from 'react-router';
import { branding } from '../../app/config/branding';
import { routes } from '../../app/config/routes';
import { sv } from '../../content/sv';
import { Container } from '../ui/Container';
import { BrandLogo } from './BrandLogo';
import styles from './Footer.module.scss';

export function Footer() {
  return (
    <footer id="kontakt" tabIndex={-1} className={styles.footer}>
      <Container>
        <div className={styles.columns}>
          <div className={styles.brand}>
            <BrandLogo />
            <p>{sv.footer.description}</p>
          </div>
          <div>
            <h2>{sv.footer.explore}</h2>
            <nav aria-label={sv.footer.explore}>
              {sv.navigation.items.slice(0, 4).map((item) => (
                <Link key={item.destination} to={routes[item.destination]}>
                  {item.label}
                </Link>
              ))}
            </nav>
          </div>
          <div>
            <h2>{sv.footer.services}</h2>
            <nav aria-label={sv.footer.services}>
              {sv.services.items.map((item) => (
                <Link key={item.id} to={`/#${item.id}`}>
                  {item.title}
                </Link>
              ))}
            </nav>
          </div>
          <div>
            <h2>{sv.footer.contact}</h2>
            <p>{sv.footer.contactPlaceholder}</p>
          </div>
        </div>
        <div className={styles.bottom}>
          <p>
            © {new Date().getFullYear()} {branding.name}
          </p>
          <div>
            <span>{sv.footer.privacy}</span>
            <span>{sv.footer.terms}</span>
          </div>
        </div>
      </Container>
    </footer>
  );
}
