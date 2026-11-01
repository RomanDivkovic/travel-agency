import { Button } from '../../components/ui/Button';
import { usePaymentCopy } from './copy';
export function PaymentError({
  status,
  retry,
}: {
  status: number;
  retry?: () => void;
}) {
  const t = usePaymentCopy();
  return (
    <div role="alert">
      <p>
        {status === 404
          ? t.unavailable
          : status === 409
            ? t.conflict
            : status === 412
              ? t.blocked
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
