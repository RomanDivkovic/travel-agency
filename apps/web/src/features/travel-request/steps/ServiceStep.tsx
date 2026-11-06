import {
  useWebsite,
  isServiceEnabled,
} from '../../business-management/contentContext';
import { serviceCodes, wizardServices } from '../../business-management/types';
import { services, type WizardData, type Service } from '../model';
import type { FormErrors } from '../validation';
import { useWizardCopy } from '../copy';
import styles from '../Wizard.module.scss';
export function ServiceStep({
  data,
  onSelect,
  errors,
}: {
  data: WizardData;
  onSelect: (service: Service) => void;
  errors: FormErrors;
}) {
  const t = useWizardCopy();
  const { documents } = useWebsite();
  return (
    <fieldset
      className={styles.serviceChoices}
      aria-describedby={errors.service ? 'service-error' : undefined}
    >
      <legend>{t.labels.service} *</legend>
      {services
        .filter((s) =>
          isServiceEnabled(
            documents,
            serviceCodes[wizardServices.indexOf(s)] || '',
          ),
        )
        .map((service) => (
          <label
            key={service}
            className={data.service === service ? styles.selected : ''}
          >
            <input
              id={`service-${service}`}
              type="radio"
              required
              aria-label={t.serviceNames[service]}
              aria-describedby={`description-${service}${errors.service ? ' service-error' : ''}`}
              name="service"
              value={service}
              checked={data.service === service}
              onChange={() => onSelect(service)}
              aria-invalid={errors.service ? true : undefined}
            />
            <span>
              <strong>{t.serviceNames[service]}</strong>
              <span id={`description-${service}`}>
                {t.serviceDescriptions[service]}
              </span>
            </span>
          </label>
        ))}
      {errors.service ? (
        <p id="service-error" className={styles.error}>
          {t.errors[errors.service]}
        </p>
      ) : null}
    </fieldset>
  );
}
