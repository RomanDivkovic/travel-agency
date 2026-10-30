import { routes } from '../../../app/config/routes';
import { sv } from '../../../content/sv';
import { ButtonLink } from '../../../components/ui/Button';
import { Icon } from '../../../components/ui/Icon';
import { Section } from '../../../components/ui/Section';
import styles from './HomeSections.module.scss';

export function FinalCTA() {
  return (
    <Section tone="navy" aria-labelledby="cta-title">
      <div className={styles.final}>
        <h2 id="cta-title">{sv.finalCta.title}</h2>
        <p>{sv.finalCta.description}</p>
        <ButtonLink
          to={routes.planTrip}
          variant="secondary"
          size="large"
          icon={<Icon name="arrow" size={20} />}
        >
          {sv.actions.plan}
        </ButtonLink>
      </div>
    </Section>
  );
}
