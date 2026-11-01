import { Button } from '../../components/ui/Button';
import { useQuotationCopy } from './copy';
export function QuotationError({
  status,
  retry,
  customer = false,
}: {
  status: number;
  retry: () => void;
  customer?: boolean;
}) {
  const t = useQuotationCopy();
  return (
    <div role="alert">
      <p>
        {status === 412
          ? t.blocked
          : status === 409
            ? t.conflict
            : status === 400 || status === 413
              ? t.invalid
              : status === 429
                ? t.rate
                : customer && status === 404
                  ? t.unavailable
                  : t.error}
      </p>
      <Button variant="outline" onClick={retry}>
        {t.retry}
      </Button>
    </div>
  );
}
