import type { Dispatch } from 'react';
import { useWizardCopy } from '../copy';
import type { StepId, WizardData } from '../model';
import type { WizardAction } from '../reducer';
import type { FormErrors } from '../validation';
import { Check, Select } from '../components/Fields';
export function PreferencesStep({
  step,
  data,
  dispatch,
  errors,
}: {
  step: StepId;
  data: WizardData;
  dispatch: Dispatch<WizardAction>;
  errors: FormErrors;
}) {
  const t = useWizardCopy();
  const l = t.labels;
  const options = (keys: readonly (keyof typeof t.choices)[]) =>
    keys.map((value) => ({ value, label: t.choices[value] }));
  if (step === 'flightPreferences')
    return (
      <>
        <p>{t.hints.preferences}</p>
        <Select
          id="priority"
          label={l.priority}
          errors={errors}
          value={data.flight.priority}
          options={options(['balanced', 'price', 'direct'])}
          onChange={(value) =>
            dispatch({
              type: 'update',
              group: 'flight',
              value: { priority: value as WizardData['flight']['priority'] },
            })
          }
        />
        <Select
          id="baggage"
          label={l.baggage}
          errors={errors}
          value={data.flight.baggage}
          options={options(['cabin', 'checked', 'none'])}
          onChange={(value) =>
            dispatch({
              type: 'update',
              group: 'flight',
              value: { baggage: value as WizardData['flight']['baggage'] },
            })
          }
        />
        <Check
          id="flexibleAirports"
          label={l.flexibleAirports}
          checked={data.flight.flexibleAirports}
          onChange={(flexibleAirports) =>
            dispatch({
              type: 'update',
              group: 'flight',
              value: { flexibleAirports },
            })
          }
        />
      </>
    );
  if (step === 'accommodationPreferences')
    return (
      <>
        <p>{t.hints.preferences}</p>
        <Select
          id="accommodationType"
          label={l.accommodationType}
          errors={errors}
          value={data.accommodation.type}
          options={options(['either', 'hotel', 'apartment'])}
          onChange={(value) =>
            dispatch({
              type: 'update',
              group: 'accommodation',
              value: { type: value as WizardData['accommodation']['type'] },
            })
          }
        />
        {(['breakfast', 'pool', 'familyFriendly'] as const).map((key) => (
          <Check
            id={key}
            key={key}
            label={l[key]}
            checked={data.accommodation[key]}
            onChange={(value) =>
              dispatch({
                type: 'update',
                group: 'accommodation',
                value: { [key]: value },
              })
            }
          />
        ))}
      </>
    );
  if (step === 'tripPreferences')
    return (
      <>
        <p>{t.hints.preferences}</p>
        <Select
          id="pace"
          label={l.pace}
          errors={errors}
          value={data.trip.pace}
          options={options(['relaxed', 'balanced', 'active']).map((option) =>
            option.value === 'balanced'
              ? { ...option, label: t.choices.balancedPace }
              : option,
          )}
          onChange={(value) =>
            dispatch({
              type: 'update',
              group: 'trip',
              value: { pace: value as WizardData['trip']['pace'] },
            })
          }
        />
        <fieldset>
          <legend>{l.interests}</legend>
          {(['beach', 'culture', 'nature', 'food'] as const).map((key) => (
            <Check
              id={`interest-${key}`}
              key={key}
              label={t.choices[key]}
              checked={data.trip.interests.includes(key)}
              onChange={(value) =>
                dispatch({
                  type: 'update',
                  group: 'trip',
                  value: {
                    interests: value
                      ? [...data.trip.interests, key]
                      : data.trip.interests.filter((item) => item !== key),
                  },
                })
              }
            />
          ))}
        </fieldset>
      </>
    );
  return null;
}
