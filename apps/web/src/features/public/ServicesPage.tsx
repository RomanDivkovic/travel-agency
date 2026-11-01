import { serviceWizardLink } from '../travel-request/config';
import { routes } from '../../app/config/routes';
import { ButtonLink } from '../../components/ui/Button';
import { Icon } from '../../components/ui/Icon';
import { Section } from '../../components/ui/Section';
import { useTranslations } from '../../localization/useLanguage';
import { PageIntro } from './PageIntro';
import { usePublicCopy } from './usePublicCopy';
import styles from './PublicPages.module.scss';
export function ServicesPage() {
  const t = useTranslations();
  const copy = usePublicCopy();
  return (
    <>
      <PageIntro {...copy.services} />
      <Section>
        <div className={styles.serviceGrid}>
          {t.services.items.map((service, index) => {
            const detail = copy.services.details[index];
            if (!detail) throw new Error('Missing service translation');
            return (
              <article
                id={service.id}
                tabIndex={-1}
                key={service.id}
                className={styles.serviceCard}
              >
                <Icon name={service.icon} size={36} />
                <h2>{service.title}</h2>
                <p>{service.description}</p>
                <h3>{copy.services.includes}</h3>
                <ul>
                  {detail.includes.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ul>
                <p>
                  <strong>{copy.services.suitable}</strong> {detail.suitable}
                </p>
                <div className={styles.serviceActions}>
                  <ButtonLink to={routes.pricing} variant="text">
                    {copy.pricingAction}
                  </ButtonLink>
                  <ButtonLink to={serviceWizardLink(service.id)}>
                    {service.action}
                  </ButtonLink>
                </div>
              </article>
            );
          })}
        </div>
      </Section>
      <Section tone="sand">
        <div className={styles.narrow}>
          <h2>{copy.services.extrasTitle}</h2>
          <p>{copy.services.extras}</p>
          <p>{copy.services.boundary}</p>
          <ButtonLink to={routes.contact}>{copy.contactAction}</ButtonLink>
        </div>
      </Section>
    </>
  );
}
