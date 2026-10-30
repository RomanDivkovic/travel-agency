import { routes } from '../app/config/routes';
import { sv } from '../content/sv';
import { ButtonLink } from '../components/ui/Button';
import { Section } from '../components/ui/Section';
import { usePageMetadata } from '../hooks/usePageMetadata';
import styles from './InformationalPage.module.scss';

export function NotFoundPage() {
  usePageMetadata(sv.notFound.title);
  return (
    <Section className={styles.page}>
      <h1>{sv.notFound.title}</h1>
      <p>{sv.notFound.description}</p>
      <ButtonLink to={routes.home}>{sv.actions.home}</ButtonLink>
    </Section>
  );
}
