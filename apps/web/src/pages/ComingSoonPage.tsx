import { routes } from '../app/config/routes';
import { sv } from '../content/sv';
import { ButtonLink } from '../components/ui/Button';
import { Section } from '../components/ui/Section';
import { usePageMetadata } from '../hooks/usePageMetadata';
import styles from './InformationalPage.module.scss';

export function ComingSoonPage({ about = false }: { about?: boolean }) {
  const title = about ? sv.comingSoon.aboutTitle : sv.comingSoon.title;
  const description = about
    ? sv.comingSoon.aboutDescription
    : sv.comingSoon.description;
  usePageMetadata(title, description);
  return (
    <Section className={styles.page}>
      <h1>{title}</h1>
      <p>{description}</p>
      {!about ? <p className={styles.note}>{sv.comingSoon.note}</p> : null}
      <ButtonLink to={routes.home} variant="outline">
        {sv.actions.home}
      </ButtonLink>
    </Section>
  );
}
