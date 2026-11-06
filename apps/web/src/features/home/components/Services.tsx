import { ServiceFee } from '../../business-management/PublishedComponents';
import type { ServiceData } from '../../business-management/types';
import {
  useWebsite,
  isServiceEnabled,
} from '../../business-management/contentContext';
import { serviceCodes, cardIds } from '../../business-management/types';
import { serviceWizardLink } from '../../travel-request/config';
import { useTranslations } from '../../../localization/useLanguage';
import { ButtonLink } from '../../../components/ui/Button';
import { Card } from '../../../components/ui/Card';
import { Icon } from '../../../components/ui/Icon';
import { Section } from '../../../components/ui/Section';
import styles from './HomeSections.module.scss';

export function Services() {
  const t = useTranslations();
  const { documents } = useWebsite();
  return (
    <Section
      id="tjanster"
      aria-labelledby="services-title"
      className={styles.servicesSection}
    >
      <div className={styles.sectionHeading}>
        <h2 id="services-title">{t.services.title}</h2>
        <p>{t.services.description}</p>
      </div>
      <div className={styles.services}>
        {t.services.items
          .filter((s) =>
            isServiceEnabled(
              documents,
              serviceCodes[cardIds.indexOf(s.id)] || '',
            ),
          )
          .map((service) => (
            <Card
              key={service.id}
              id={service.id}
              tabIndex={-1}
              className={styles.service}
            >
              <Icon name={service.icon} size={38} />
              <h3>{service.title}</h3>
              <p>{service.description}</p>
              {documents[
                'service:' + serviceCodes[cardIds.indexOf(service.id)]
              ] && (
                <p>
                  <ServiceFee
                    service={
                      documents[
                        'service:' + serviceCodes[cardIds.indexOf(service.id)]
                      ] as ServiceData
                    }
                  />
                </p>
              )}
              <ButtonLink
                to={serviceWizardLink(service.id)}
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
