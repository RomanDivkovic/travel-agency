import { routes } from '../../../app/config/routes';
import { sv } from '../../../content/sv';
import { ButtonLink } from '../../../components/ui/Button';
import { Icon } from '../../../components/ui/Icon';
import { Section } from '../../../components/ui/Section';
import styles from './HomeSections.module.scss';

export function AboutPreview() {
  return (
    <Section id="om-mig" tone="sand" aria-labelledby="about-title">
      <div className={styles.about}>
        <div className={styles.portrait}>
          <Icon name="user" size={64} />
          <p>{sv.about.portrait}</p>
        </div>
        <div className={styles.aboutCopy}>
          <h2 id="about-title">{sv.about.title}</h2>
          <p>{sv.about.description}</p>
          <ButtonLink
            to={routes.aboutPreview}
            variant="outline"
            icon={<Icon name="arrow" size={20} />}
          >
            {sv.actions.about}
          </ButtonLink>
        </div>
      </div>
    </Section>
  );
}
