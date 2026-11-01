import { routes } from '../../../app/config/routes';
import { useTranslations } from '../../../localization/useLanguage';
import { ButtonLink } from '../../../components/ui/Button';
import { Icon } from '../../../components/ui/Icon';
import { Section } from '../../../components/ui/Section';
import styles from './HomeSections.module.scss';

export function FinalCTA() {
  const t = useTranslations();
  return (
    <Section tone="navy" aria-labelledby="cta-title">
      <div className={styles.final}>
        <h2 id="cta-title">{t.finalCta.title}</h2>
        <p>{t.finalCta.description}</p>
        <ButtonLink
          to={routes.planTrip}
          variant="secondary"
          size="large"
          icon={<Icon name="arrow" size={20} />}
        >
          {t.actions.plan}
        </ButtonLink>
      </div>
    </Section>
  );
}
