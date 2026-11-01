import type { Dispatch } from 'react';
import { hasAccommodation, hasFlights } from '../config';
import { useWizardCopy } from '../copy';
import type { WizardData, StepId, Currency } from '../model';
import type { WizardAction } from '../reducer';
import type { FormErrors } from '../validation';
import { localToday } from '../validation';
import { Check, Input, Field, Select } from '../components/Fields';
import styles from '../Wizard.module.scss';
export function TripDetails({
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
  if (step === 'destination')
    return (
      <>
        <p>{t.hints.destination}</p>
        {hasFlights(data.service) ? (
          <Input
            id="origin"
            label={l.origin}
            errors={errors}
            value={data.destination.origin}
            maxLength={150}
            autoComplete="off"
            onChange={(e) =>
              dispatch({
                type: 'update',
                group: 'destination',
                value: { origin: e.target.value },
              })
            }
          />
        ) : null}
        <Check
          id="flexibleDestination"
          label={l.flexibleDestination}
          checked={data.destination.flexible}
          onChange={(flexible) =>
            dispatch({
              type: 'update',
              group: 'destination',
              value: { flexible },
            })
          }
        />
        <Input
          id="place"
          label={l.place}
          errors={errors}
          optional={data.destination.flexible}
          value={data.destination.place}
          maxLength={150}
          onChange={(e) =>
            dispatch({
              type: 'update',
              group: 'destination',
              value: { place: e.target.value },
            })
          }
        />
        {data.service === 'complete' ? (
          <Field id="stops" label={l.stops} errors={errors} optional>
            <textarea
              id="stops"
              rows={3}
              maxLength={500}
              value={data.destination.stops}
              onChange={(e) =>
                dispatch({
                  type: 'update',
                  group: 'destination',
                  value: { stops: e.target.value },
                })
              }
              aria-invalid={errors.stops ? true : undefined}
              aria-describedby={errors.stops ? 'stops-error' : undefined}
            />
          </Field>
        ) : null}
      </>
    );
  if (step === 'dates') {
    const d = data.dates;
    const dateField = (
      id: 'departure' | 'return' | 'earliest' | 'latest',
      label: string,
    ) => (
      <Input
        id={id}
        label={label}
        errors={errors}
        type="date"
        min={localToday()}
        value={d[id]}
        onChange={(e) =>
          dispatch({
            type: 'update',
            group: 'dates',
            value: { [id]: e.target.value },
          })
        }
      />
    );
    return (
      <>
        <p>{t.hints.dates}</p>
        <Select
          id="dateMode"
          label={l.dateMode}
          errors={errors}
          value={d.mode}
          options={[
            { value: 'fixed', label: l.fixed },
            { value: 'flexible', label: l.flexible },
          ]}
          onChange={(value) =>
            dispatch({
              type: 'update',
              group: 'dates',
              value: { mode: value === 'flexible' ? 'flexible' : 'fixed' },
            })
          }
        />
        {d.mode === 'fixed' ? (
          <>
            {data.service === 'flights' ? (
              <Check
                id="oneWay"
                label={l.oneWay}
                checked={d.oneWay}
                onChange={(oneWay) =>
                  dispatch({
                    type: 'update',
                    group: 'dates',
                    value: { oneWay },
                  })
                }
              />
            ) : null}
            <div className={styles.twoColumns}>
              {dateField(
                'departure',
                data.service === 'accommodation' ? l.checkIn : l.departure,
              )}
              {!(data.service === 'flights' && d.oneWay)
                ? dateField(
                    'return',
                    data.service === 'accommodation' ? l.checkOut : l.return,
                  )
                : null}
            </div>
          </>
        ) : (
          <>
            <div className={styles.twoColumns}>
              {dateField('earliest', l.earliest)}
              {dateField('latest', l.latest)}
            </div>
            <Input
              id="duration"
              label={l.duration}
              errors={errors}
              type="number"
              min={1}
              max={90}
              value={d.duration}
              onChange={(e) =>
                dispatch({
                  type: 'update',
                  group: 'dates',
                  value: { duration: e.target.value },
                })
              }
            />
          </>
        )}
      </>
    );
  }
  if (step === 'travelers')
    return (
      <>
        <p>{t.hints.travelers}</p>
        <div className={styles.twoColumns}>
          {(['adults', 'children'] as const).map((key) => (
            <Input
              key={key}
              id={key}
              label={l[key]}
              errors={errors}
              type="number"
              min={key === 'adults' ? 1 : 0}
              max={key === 'adults' ? 20 : 10}
              value={data.travelers[key]}
              onChange={(e) =>
                dispatch({
                  type: 'update',
                  group: 'travelers',
                  value: { [key]: e.target.value },
                })
              }
            />
          ))}
        </div>
        <div className={styles.twoColumns}>
          {data.travelers.ages.map((age, index) => (
            <Input
              key={index}
              id={`age-${index}`}
              label={`${l.age} ${index + 1}`}
              errors={errors}
              type="number"
              min={0}
              max={17}
              value={age}
              onChange={(e) =>
                dispatch({
                  type: 'update',
                  group: 'travelers',
                  value: {
                    ages: data.travelers.ages.map((old, i) =>
                      i === index ? e.target.value : old,
                    ),
                  },
                })
              }
            />
          ))}
        </div>
        {hasAccommodation(data.service) ? (
          <Input
            id="rooms"
            label={l.rooms}
            errors={errors}
            type="number"
            min={1}
            max={20}
            value={data.travelers.rooms}
            onChange={(e) =>
              dispatch({
                type: 'update',
                group: 'travelers',
                value: { rooms: e.target.value },
              })
            }
          />
        ) : null}
      </>
    );
  if (step === 'budget')
    return (
      <>
        <p>{t.hints.budget}</p>
        <Input
          id="amount"
          label={l.amount}
          errors={errors}
          type="number"
          min="0.01"
          max={100000000}
          step="0.01"
          value={data.budget.amount}
          onChange={(e) =>
            dispatch({
              type: 'update',
              group: 'budget',
              value: { amount: e.target.value },
            })
          }
        />
        <div className={styles.twoColumns}>
          <Select
            id="currency"
            label={l.currency}
            errors={errors}
            value={data.budget.currency}
            options={(['SEK', 'EUR', 'USD'] as const).map((value) => ({
              value,
              label: value,
            }))}
            onChange={(value) =>
              dispatch({
                type: 'update',
                group: 'budget',
                value: { currency: value as Currency },
              })
            }
          />
          <Select
            id="scope"
            label={l.scope}
            errors={errors}
            value={data.budget.scope}
            options={(['total', 'per-person'] as const).map((value) => ({
              value,
              label: t.choices[value],
            }))}
            onChange={(value) =>
              dispatch({
                type: 'update',
                group: 'budget',
                value: {
                  scope: value === 'per-person' ? 'per-person' : 'total',
                },
              })
            }
          />
        </div>
      </>
    );
  return null;
}
