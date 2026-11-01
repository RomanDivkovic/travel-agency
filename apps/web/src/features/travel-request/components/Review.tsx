import { Button } from '../../../components/ui/Button';
import { useLanguage } from '../../../localization/useLanguage';
import { useWizardCopy } from '../copy';
import { hasAccommodation, hasFlights, wizardSteps } from '../config';
import type { StepId, WizardData } from '../model';
import { buildTravelRequest } from '../payload';
import styles from '../Wizard.module.scss';
export function Review({
  data,
  onEdit,
}: {
  data: WizardData;
  onEdit: (step: StepId) => void;
}) {
  const t = useWizardCopy();
  const l = t.labels;
  const { language } = useLanguage();
  const payload = buildTravelRequest(data, language);
  const yes = (value: boolean) => (value ? t.yes : t.no);
  function rows(step: StepId): [string, string][] {
    switch (step) {
      case 'service':
        return [
          [l.service, data.service ? t.serviceNames[data.service] : t.noAnswer],
        ];
      case 'destination':
        return [
          ...(hasFlights(data.service)
            ? [[l.origin, data.destination.origin] as [string, string]]
            : []),
          [l.place, data.destination.place],
          [l.flexibleDestination, yes(data.destination.flexible)],
          ...(data.service === 'complete'
            ? [[l.stops, data.destination.stops] as [string, string]]
            : []),
        ];
      case 'dates':
        return data.dates.mode === 'fixed'
          ? [
              [l.dateMode, l.fixed],
              [
                data.service === 'accommodation' ? l.checkIn : l.departure,
                data.dates.departure,
              ],
              ...(data.service === 'flights' && data.dates.oneWay
                ? [[l.oneWay, t.yes] as [string, string]]
                : [
                    [
                      data.service === 'accommodation' ? l.checkOut : l.return,
                      data.dates.return,
                    ] as [string, string],
                  ]),
            ]
          : [
              [l.dateMode, l.flexible],
              [l.earliest, data.dates.earliest],
              [l.latest, data.dates.latest],
              [l.duration, data.dates.duration],
            ];
      case 'travelers':
        return [
          [l.adults, data.travelers.adults],
          [l.children, data.travelers.children],
          ...data.travelers.ages.map(
            (age, index) => [`${l.age} ${index + 1}`, age] as [string, string],
          ),
          ...(hasAccommodation(data.service)
            ? [[l.rooms, data.travelers.rooms] as [string, string]]
            : []),
        ];
      case 'budget':
        return [
          [
            l.amount,
            new Intl.NumberFormat(language === 'sv' ? 'sv-SE' : 'en-GB', {
              style: 'currency',
              currency: data.budget.currency,
            }).format(Number(data.budget.amount)),
          ],
          [l.scope, t.choices[data.budget.scope]],
        ];
      case 'flightPreferences':
        return [
          [l.priority, t.choices[data.flight.priority]],
          [l.baggage, t.choices[data.flight.baggage]],
          [l.flexibleAirports, yes(data.flight.flexibleAirports)],
        ];
      case 'accommodationPreferences':
        return [
          [l.accommodationType, t.choices[data.accommodation.type]],
          [l.breakfast, yes(data.accommodation.breakfast)],
          [l.pool, yes(data.accommodation.pool)],
          [l.familyFriendly, yes(data.accommodation.familyFriendly)],
        ];
      case 'tripPreferences':
        return [
          [
            l.pace,
            data.trip.pace === 'balanced'
              ? t.choices.balancedPace
              : t.choices[data.trip.pace],
          ],
          [
            l.interests,
            data.trip.interests.map((key) => t.choices[key]).join(', '),
          ],
        ];
      case 'additional':
        return [
          [l.activities, yes(data.additional.activities)],
          [l.special, data.additional.message],
        ];
      case 'contact':
        return [
          [l.name, data.contact.name],
          [l.email, data.contact.email],
          [l.phone, data.contact.phone],
          [l.contactMethod, t.choices[data.contact.method]],
        ];
      default:
        return [];
    }
  }
  return (
    <>
      <p className={styles.notice}>{t.reviewNote}</p>
      {payload ? <p role="status">{t.checked}</p> : null}
      <div className={styles.review}>
        {wizardSteps(data.service)
          .filter((step) => step !== 'review')
          .map((step) => (
            <section key={step} aria-labelledby={`review-${step}`}>
              <div className={styles.reviewHeading}>
                <h3 id={`review-${step}`}>{t.steps[step]}</h3>
                <Button
                  variant="text"
                  onClick={() => onEdit(step)}
                  aria-label={`${t.edit}: ${t.steps[step]}`}
                >
                  {t.edit}
                </Button>
              </div>
              <dl>
                {rows(step).map(([label, value]) => (
                  <div key={label}>
                    <dt>{label}</dt>
                    <dd>{value.trim() || t.noAnswer}</dd>
                  </div>
                ))}
              </dl>
            </section>
          ))}
      </div>
      <Button disabled aria-describedby="submission-notice">
        {t.submitLater}
      </Button>
      <p id="submission-notice">{t.unavailable}</p>
    </>
  );
}
