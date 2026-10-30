import { routes } from '../../../app/config/routes';
import { sv } from '../../../content/sv';
import { ButtonLink } from '../../../components/ui/Button';
import { Card } from '../../../components/ui/Card';
import { Icon } from '../../../components/ui/Icon';
import { Section } from '../../../components/ui/Section';
import styles from './HomeSections.module.scss';

export function Services() {
  return (
    <Section
      id="tjanster"
      aria-labelledby="services-title"
      className={styles.servicesSection}
    >
      <div className={styles.sectionHeading}>
        <h2 id="services-title">{sv.services.title}</h2>
        <p>{sv.services.description}</p>
      </div>
      <div className={styles.services}>
        {sv.services.items.map((service) => (
          <Card
            key={service.id}
            id={service.id}
            tabIndex={-1}
            className={styles.service}
          >
            <Icon name={service.icon} size={38} />
            <h3>{service.title}</h3>
            <p>{service.description}</p>
            <ButtonLink
              to={routes.planTrip}
              variant="text"
              icon={<Icon name="arrow" size={18} />}
            >
              {service.action}
            </ButtonLink>
          </Card>
        ))}
      </div>
    </Section>
  );
}
