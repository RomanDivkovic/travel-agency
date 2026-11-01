import { routes } from '../app/config/routes';
import { useTranslations } from '../localization/useLanguage';
import { ButtonLink } from '../components/ui/Button';
import { Section } from '../components/ui/Section';
import { usePageMetadata } from '../hooks/usePageMetadata';
import styles from './InformationalPage.module.scss';

export function NotFoundPage() {
  const t = useTranslations();
  usePageMetadata(t.notFound.title);
  return (
    <Section className={styles.page}>
      <h1>{t.notFound.title}</h1>
      <p>{t.notFound.description}</p>
      <ButtonLink to={routes.home}>{t.actions.home}</ButtonLink>
    </Section>
  );
}
