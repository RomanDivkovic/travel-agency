import { routes } from '../../../app/config/routes';
import { useTranslations } from '../../../localization/useLanguage';
import { ButtonLink } from '../../../components/ui/Button';
import { Icon } from '../../../components/ui/Icon';
import { Section } from '../../../components/ui/Section';
import styles from './HomeSections.module.scss';

export function AboutPreview() {
  const t = useTranslations();
  return (
    <Section id="om-mig" tone="sand" aria-labelledby="about-title">
      <div className={styles.about}>
        <div className={styles.portrait}>
          <Icon name="user" size={64} />
          <p>{t.about.portrait}</p>
        </div>
        <div className={styles.aboutCopy}>
          <h2 id="about-title">{t.about.title}</h2>
          <p>{t.about.description}</p>
          <ButtonLink
            to={routes.about}
            variant="outline"
            icon={<Icon name="arrow" size={20} />}
          >
            {t.actions.about}
          </ButtonLink>
        </div>
      </div>
    </Section>
  );
}
