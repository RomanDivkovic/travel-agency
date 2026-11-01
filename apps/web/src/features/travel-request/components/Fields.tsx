import type { InputHTMLAttributes, ReactNode } from 'react';
import type { FormErrors } from '../validation';
import { useWizardCopy } from '../copy';
import styles from '../Wizard.module.scss';
export function Field({
  id,
  label,
  errors,
  children,
  optional = false,
}: {
  id: string;
  label: string;
  errors: FormErrors;
  children: ReactNode;
  optional?: boolean;
}) {
  const t = useWizardCopy();
  const error = errors[id];
  return (
    <div className={styles.field}>
      <label htmlFor={id}>
        {label}{' '}
        {optional ? (
          <span>({t.optional})</span>
        ) : (
          <span aria-hidden="true">*</span>
        )}
      </label>
      {children}
      {error ? (
        <p className={styles.error} id={`${id}-error`}>
          {t.errors[error]}
        </p>
      ) : null}
    </div>
  );
}
export function Input({
  id,
  label,
  errors,
  optional,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & {
  id: string;
  label: string;
  errors: FormErrors;
  optional?: boolean;
}) {
  return (
    <Field id={id} label={label} errors={errors} optional={optional}>
      <input
        {...props}
        id={id}
        required={!optional}
        aria-invalid={errors[id] ? true : undefined}
        aria-describedby={errors[id] ? `${id}-error` : undefined}
      />
    </Field>
  );
}
export function Select({
  id,
  label,
  errors,
  options,
  value,
  onChange,
}: {
  id: string;
  label: string;
  errors: FormErrors;
  options: readonly { value: string; label: string }[];
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <Field id={id} label={label} errors={errors}>
      <select
        id={id}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        required
        aria-invalid={errors[id] ? true : undefined}
        aria-describedby={errors[id] ? `${id}-error` : undefined}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </Field>
  );
}
export function Check({
  id,
  label,
  checked,
  onChange,
}: {
  id: string;
  label: string;
  checked: boolean;
  onChange: (value: boolean) => void;
}) {
  return (
    <label className={styles.check} htmlFor={id}>
      <input
        id={id}
        type="checkbox"
        checked={checked}
        onChange={(event) => onChange(event.target.checked)}
      />
      <span>{label}</span>
    </label>
  );
}
