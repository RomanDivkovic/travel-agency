import { Button } from '../../components/ui/Button';
import { useResearchCopy } from './copy';
import styles from './Research.module.scss';
export function ResearchError({
  status,
  retry,
}: {
  status: number;
  retry?: () => void;
}) {
  const t = useResearchCopy();
  return (
    <div role="alert">
      <p>
        {status === 403
          ? t.eligibility
          : status === 404
            ? t.unavailable
            : status === 409
              ? t.conflict
              : status === 412
                ? t.blocked
                : status === 400
                  ? t.invalid
                  : t.error}
      </p>
      {retry && (
        <Button variant="outline" onClick={retry}>
          {t.retry}
        </Button>
      )}
    </div>
  );
}
export function TextField({
  label,
  value,
  onChange,
  type = 'text',
  required = true,
  maxLength = 200,
  min,
  max,
  step,
}: {
  label: string;
  value: string | number;
  onChange: (value: string) => void;
  type?: string;
  required?: boolean;
  maxLength?: number;
  min?: number;
  max?: number;
  step?: string;
}) {
  return (
    <label>
      {label}
      <input
        aria-label={label}
        type={type}
        value={value}
        required={required}
        maxLength={maxLength}
        min={min}
        max={max}
        step={step}
        onChange={(e) => onChange(e.target.value)}
      />
    </label>
  );
}
export function TextArea({
  label,
  value,
  onChange,
  maxLength,
  required = false,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  maxLength: number;
  required?: boolean;
}) {
  return (
    <label>
      {label}
      <textarea
        aria-label={label}
        value={value}
        required={required}
        maxLength={maxLength}
        onChange={(e) => onChange(e.target.value)}
      />
    </label>
  );
}
export function ResearchNotice() {
  const t = useResearchCopy();
  return (
    <>
      <p className={styles.notice}>{t.priceNotice}</p>
      <p>{t.noBooking}</p>
    </>
  );
}
