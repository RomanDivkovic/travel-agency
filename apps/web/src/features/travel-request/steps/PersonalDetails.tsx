import type { Dispatch } from 'react';
import { useWizardCopy } from '../copy';
import type { StepId, WizardData } from '../model';
import type { WizardAction } from '../reducer';
import type { FormErrors } from '../validation';
import { Check, Field, Input, Select } from '../components/Fields';
export function PersonalDetails({
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
  if (step === 'additional')
    return (
      <>
        <Check
          id="activities"
          label={l.activities}
          checked={data.additional.activities}
          onChange={(activities) =>
            dispatch({
              type: 'update',
              group: 'additional',
              value: { activities },
            })
          }
        />
        <p id="special-hint">{t.hints.additional}</p>
        <Field id="special" label={l.special} errors={errors} optional>
          <textarea
            id="special"
            value={data.additional.message}
            rows={6}
            maxLength={2000}
            onChange={(e) =>
              dispatch({
                type: 'update',
                group: 'additional',
                value: { message: e.target.value },
              })
            }
            aria-invalid={errors.special ? true : undefined}
            aria-describedby={
              errors.special ? 'special-hint special-error' : 'special-hint'
            }
          />
        </Field>
      </>
    );
  if (step === 'contact')
    return (
      <>
        <p>{t.hints.contact}</p>
        <Input
          id="name"
          label={l.name}
          errors={errors}
          value={data.contact.name}
          autoComplete="name"
          maxLength={100}
          onChange={(e) =>
            dispatch({
              type: 'update',
              group: 'contact',
              value: { name: e.target.value },
            })
          }
        />
        <Input
          id="email"
          label={l.email}
          errors={errors}
          value={data.contact.email}
          type="email"
          autoComplete="email"
          maxLength={254}
          onChange={(e) =>
            dispatch({
              type: 'update',
              group: 'contact',
              value: { email: e.target.value },
            })
          }
        />
        <Select
          id="contactMethod"
          label={l.contactMethod}
          errors={errors}
          value={data.contact.method}
          options={(['email', 'phone'] as const).map((value) => ({
            value,
            label: t.choices[value],
          }))}
          onChange={(value) =>
            dispatch({
              type: 'update',
              group: 'contact',
              value: { method: value === 'phone' ? 'phone' : 'email' },
            })
          }
        />
        <Input
          id="phone"
          label={l.phone}
          errors={errors}
          value={data.contact.phone}
          type="tel"
          autoComplete="tel"
          maxLength={40}
          optional={data.contact.method !== 'phone'}
          onChange={(e) =>
            dispatch({
              type: 'update',
              group: 'contact',
              value: { phone: e.target.value },
            })
          }
        />
      </>
    );
  return null;
}
