import { routes } from '../app/config/routes';
import { useTranslations } from '../localization/useLanguage';
import { ButtonLink } from '../components/ui/Button';
import { Section } from '../components/ui/Section';
import { usePageMetadata } from '../hooks/usePageMetadata';
import styles from './InformationalPage.module.scss';

export function ComingSoonPage({ about = false }: { about?: boolean }) {
  const t = useTranslations();
  const title = about ? t.comingSoon.aboutTitle : t.comingSoon.title;
  const description = about
    ? t.comingSoon.aboutDescription
    : t.comingSoon.description;
  usePageMetadata(title, description);
  return (
    <Section className={styles.page}>
      <h1>{title}</h1>
      <p>{description}</p>
      {!about ? <p className={styles.note}>{t.comingSoon.note}</p> : null}
      <ButtonLink to={routes.home} variant="outline">
        {t.actions.home}
      </ButtonLink>
    </Section>
  );
}
