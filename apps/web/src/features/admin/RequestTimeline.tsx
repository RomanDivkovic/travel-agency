import { useBookingCopy } from '../bookings/copy';
import { useResearchCopy } from '../travel-research/copy';
import { usePaymentCopy } from '../payments/copy';
import { useDateFormat } from './useDateFormat';
import { useCallback, useState } from 'react';
import { quotationEventLabel, useQuotationCopy } from '../quotations/copy';
import { adminApi } from './api';
import { useAdminResource } from './useAdminResource';
import { useAdminCopy } from './copy';
import { ErrorState, Pagination } from './AdminShared';
import styles from './Admin.module.scss';
export function RequestTimeline({
  id,
  revision,
  kind,
}: {
  id: string;
  revision: number;
  kind: 'notes' | 'activity';
}) {
  const t = useAdminCopy();
  const bookings = useBookingCopy();
  const payments = usePaymentCopy();
  const research = useResearchCopy();
  const quotations = useQuotationCopy();
  const statusLabel = (value: string | null) =>
    value && Object.hasOwn(bookings.statuses, value)
      ? bookings.statuses[value as keyof typeof bookings.statuses]
      : value && Object.hasOwn(t.statuses, value)
        ? t.statuses[value as keyof typeof t.statuses]
        : value;
  const date = useDateFormat();
  const [page, setPage] = useState(1);
  const [retry, setRetry] = useState(0);
  const load = useCallback(
    async (signal: AbortSignal) => {
      if (kind === 'notes') {
        const r = await adminApi.notes(id, page, signal);
        return {
          ...r,
          items: r.items.map((n) => ({
            id: n.id,
            content: n.content,
            author: n.author,
            createdAt: n.createdAt,
            type: '' as string,
            previousValue: null,
            newValue: null,
          })),
        };
      }
      const r = await adminApi.activity(id, page, signal);
      return { ...r, items: r.items.map((a) => ({ ...a, content: '' })) };
    },
    [id, page, kind],
  );
  const state = useAdminResource(
    `${id}-${kind}-${revision}-${page}-${retry}`,
    load,
  );
  return (
    <section className={styles.panel}>
      <h2>{kind === 'notes' ? t.notes : t.history}</h2>
      {state.error !== undefined ? (
        <ErrorState status={state.error} retry={() => setRetry((v) => v + 1)} />
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <>
          {state.data.items.length ? (
            <ol className={styles.timeline}>
              {state.data.items.map((item) => (
                <li key={item.id}>
                  <p>
                    {item.type === 'Submitted'
                      ? t.requestSubmitted
                      : item.type === 'NoteAdded'
                        ? t.noteAdded
                        : item.type === 'StatusChanged'
                          ? `${t.statusChanged}: ${item.previousValue ? statusLabel(item.previousValue) : ''} → ${item.newValue ? statusLabel(item.newValue) : ''}`
                          : item.type === 'BookingUpdated'
                            ? bookings.workspace
                            : item.type === 'BookingMailSent'
                              ? bookings.delivered
                              : item.type
                                ? Object.hasOwn(research.events, item.type)
                                  ? research.events[
                                      item.type as keyof typeof research.events
                                    ]
                                  : Object.hasOwn(payments.events, item.type)
                                    ? payments.events[
                                        item.type as keyof typeof payments.events
                                      ]
                                    : quotationEventLabel(item.type, quotations)
                                : null}
                  </p>
                  {item.content ? (
                    <p className={styles.prose}>{item.content}</p>
                  ) : null}
                  <small>
                    {item.author ??
                      (item.type === 'QuotationExpired' ||
                      item.type === 'ProposalExpired' ||
                      [
                        'InvoiceOverdue',
                        'ServiceFeeVerified',
                        'PaymentFailed',
                        'PaymentCancelled',
                      ].includes(item.type)
                        ? quotations.system
                        : t.customer)}{' '}
                    ·{' '}
                    <time dateTime={item.createdAt}>
                      {date(item.createdAt)}
                    </time>
                  </small>
                </li>
              ))}
            </ol>
          ) : (
            <p>{kind === 'notes' ? t.noNotes : t.noHistory}</p>
          )}
          {state.data.totalPages > 1 ? (
            <Pagination {...state.data} onPage={setPage} />
          ) : null}
        </>
      )}
    </section>
  );
}
