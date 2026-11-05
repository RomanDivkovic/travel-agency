import { ConversationContact } from '../messaging/MessagingIntegration';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router';
import { Button } from '../../components/ui/Button';
import { useApiResource } from '../../hooks/useApiResource';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { ApiError } from '../../services/apiClient';
import { useDocumentCopy } from '../travel-documents/copy';
import { useDateFormat } from '../admin/useDateFormat';
import { RequestFields } from '../admin/RequestFields';
import { QuotationSummary } from '../quotations/QuotationSummary';
import { CustomerQuotationDecision } from '../quotations/CustomerQuotationPage';
import { InvoiceSummary } from '../payments/InvoiceSummary';
import { usePaymentCopy } from '../payments/copy';
import { safeCheckoutUrl } from '../payments/api';
import { ProposalSummary } from '../travel-research/ProposalSummary';
import { ProposalDecision } from '../travel-research/ProposalDecision';
import { ResearchOptionSummary } from '../travel-research/ResearchOptionSummary';
import { customerPortalApi, downloadCustomerInvoice } from './api';
import { useCustomerCopy } from './copy';
import {
  PortalLoad,
  PortalCards,
  JourneyProgress,
  ActivityList,
  CustomerPagination,
  CustomerError,
} from './PortalShared';
import type {
  ResourceKind,
  CustomerQuotation,
  CustomerProposal,
} from './types';
import styles from './Customer.module.scss';
function Timeline({ id }: { id: string }) {
  const t = useCustomerCopy();
  const [page, setPage] = useState(1),
    [retry, setRetry] = useState(0);
  const load = useCallback(
    (s: AbortSignal) => customerPortalApi.activity(id, page, s),
    [id, page],
  );
  const state = useApiResource(`${id}-${page}-${retry}`, load);
  return (
    <section className={styles.panel}>
      <h2>{t.activity}</h2>
      <PortalLoad
        loading={!state.data}
        error={state.error}
        retry={() => setRetry((v) => v + 1)}
      >
        {state.data && (
          <>
            <ActivityList items={state.data.items} />
            <CustomerPagination {...state.data} onPage={setPage} />
          </>
        )}
      </PortalLoad>
    </section>
  );
}
function RequestPage({ id }: { id: string }) {
  const t = useCustomerCopy();
  usePageMetadata(t.requests);
  const [revision, setRevision] = useState(0);
  const load = useCallback(
    (s: AbortSignal) => customerPortalApi.request(id, s),
    [id],
  );
  const state = useApiResource(`${id}-${revision}`, load);
  const d = state.data;
  return (
    <>
      <Link to="/account/requests">← {t.requests}</Link>
      <h1>{d?.request.reference ?? t.requests}</h1>
      <PortalLoad
        error={state.error}
        loading={!d}
        retry={() => setRevision((v) => v + 1)}
      >
        {d && (
          <>
            <p>{t.services[d.request.service]}</p>
            <p className={styles.badge}>{t.statuses[d.request.status]}</p>
            <JourneyProgress journey={d.request.journey} />
            <RequestFields request={d.request} />
            <ConversationContact kind="request" id={id} />
            <section className={styles.panel}>
              <h2>{t.related}</h2>
              <PortalCards items={d.related} />
            </section>
            <Timeline id={id} />
          </>
        )}
      </PortalLoad>
    </>
  );
}
function QuotationPage({ id }: { id: string }) {
  const t = useCustomerCopy();
  usePageMetadata(t.quotations);
  const [revision, setRevision] = useState(0),
    [confirmed, setConfirmed] = useState<CustomerQuotation>();
  const heading = useRef<HTMLHeadingElement>(null);
  const load = useCallback(
    (s: AbortSignal) => customerPortalApi.quotation(id, s),
    [id],
  );
  const state = useApiResource(`${id}-${revision}`, load);
  const q = confirmed ?? state.data;
  function refresh() {
    setConfirmed(undefined);
    setRevision((v) => v + 1);
  }
  useEffect(() => {
    if (confirmed) heading.current?.focus();
  }, [confirmed]);
  return (
    <>
      <Link to="/account/quotations">← {t.quotations}</Link>
      <h1 ref={heading} tabIndex={-1}>
        {t.quotations}
      </h1>
      <PortalLoad error={state.error} loading={!q} retry={refresh}>
        {q && (
          <>
            <p role="status">{t.statuses[q.status]}</p>
            <QuotationSummary quotation={q} />
            {q.status === 'Sent' && (
              <CustomerQuotationDecision
                key={`${q.number}-${q.version}`}
                quotation={q}
                token=""
                onConfirmed={setConfirmed}
                onRefresh={refresh}
                onRespond={(accept) =>
                  customerPortalApi.respondQuotation(id, accept)
                }
              />
            )}
          </>
        )}
      </PortalLoad>
    </>
  );
}
function InvoicePage({ id }: { id: string }) {
  const t = useCustomerCopy(),
    payments = usePaymentCopy();
  usePageMetadata(t.invoices);
  const [revision, setRevision] = useState(0),
    [busy, setBusy] = useState(false),
    [error, setError] = useState<number>();
  const pending = useRef(false);
  const load = useCallback(
    (s: AbortSignal) => customerPortalApi.invoice(id, s),
    [id],
  );
  const state = useApiResource(`${id}-${revision}`, load);
  const invoice = state.data;
  useEffect(() => {
    if (invoice?.status !== 'PaymentPending') return;
    let count = 0;
    const timer = setInterval(() => {
      if (++count <= 12) setRevision((v) => v + 1);
      else clearInterval(timer);
    }, 5000);
    return () => clearInterval(timer);
  }, [invoice?.status]);
  async function pay() {
    if (!invoice || pending.current) return;
    pending.current = true;
    setBusy(true);
    setError(undefined);
    try {
      const r = await customerPortalApi.payment(id);
      window.location.assign(
        safeCheckoutUrl(r.checkoutUrl, invoice.isSimulated),
      );
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
      setBusy(false);
      pending.current = false;
    }
  }
  return (
    <>
      <Link to="/account/invoices">← {t.invoices}</Link>
      <h1>{t.invoices}</h1>
      <PortalLoad
        error={state.error}
        loading={!invoice}
        retry={() => setRevision((v) => v + 1)}
      >
        {invoice && (
          <>
            <InvoiceSummary invoice={invoice} />
            <div className={styles.actions}>
              {['Issued', 'PaymentPending', 'Overdue'].includes(
                invoice.status,
              ) && (
                <Button disabled={busy} onClick={() => void pay()}>
                  {payments.pay}
                </Button>
              )}
              <Button
                variant="outline"
                disabled={busy}
                onClick={() => {
                  setError(undefined);
                  void downloadCustomerInvoice(id).catch((e: unknown) =>
                    setError(e instanceof ApiError ? (e.status ?? 0) : 0),
                  );
                }}
              >
                {payments.download}
              </Button>
              <Button
                variant="outline"
                onClick={() => setRevision((v) => v + 1)}
              >
                {t.retry}
              </Button>
            </div>
            {error !== undefined && <CustomerError status={error} />}
          </>
        )}
      </PortalLoad>
    </>
  );
}
function ProposalPage({ id }: { id: string }) {
  const t = useCustomerCopy();
  usePageMetadata(t.proposals);
  const [revision, setRevision] = useState(0),
    [confirmed, setConfirmed] = useState<CustomerProposal>();
  const heading = useRef<HTMLHeadingElement>(null);
  const load = useCallback(
    (s: AbortSignal) => customerPortalApi.proposal(id, s),
    [id],
  );
  const state = useApiResource(`${id}-${revision}`, load);
  const p = confirmed ?? state.data;
  function refresh() {
    setConfirmed(undefined);
    setRevision((v) => v + 1);
  }
  useEffect(() => {
    if (confirmed) heading.current?.focus();
  }, [confirmed]);
  return (
    <>
      <Link to="/account/proposals">← {t.proposals}</Link>
      <h1 ref={heading} tabIndex={-1}>
        {t.proposals}
      </h1>
      <PortalLoad error={state.error} loading={!p} retry={refresh}>
        {p && (
          <>
            <p role="status">{t.statuses[p.status]}</p>
            <ProposalSummary proposal={p} />
            {p.canRespond && (
              <ProposalDecision
                key={`${p.number}-${p.version}`}
                proposal={p}
                token=""
                onConfirmed={setConfirmed}
                onRefresh={refresh}
                onRespond={(approve, alternative, feedback) =>
                  customerPortalApi.respondProposal(
                    id,
                    p,
                    approve,
                    alternative,
                    feedback,
                  )
                }
              />
            )}
          </>
        )}
      </PortalLoad>
    </>
  );
}
function BookingPage({ id }: { id: string }) {
  const docs = useDocumentCopy();
  const t = useCustomerCopy();
  const date = useDateFormat();
  usePageMetadata(t.bookings);
  const [revision, setRevision] = useState(0);
  const load = useCallback(
    (s: AbortSignal) => customerPortalApi.booking(id, s),
    [id],
  );
  const state = useApiResource(`${id}-${revision}`, load);
  const b = state.data;
  return (
    <>
      <Link to="/account/bookings">← {t.bookings}</Link>
      <nav className={styles.actions}>
        <Link to={'/account/bookings/' + id + '/documents'}>
          {docs.documents}
        </Link>
        <Link to={'/account/bookings/' + id + '/itinerary'}>
          {docs.itinerary}
        </Link>
      </nav>
      <h1>{b?.reference ?? t.bookings}</h1>
      {b && <ConversationContact kind="booking" id={id} />}
      <PortalLoad
        error={state.error}
        loading={!b}
        retry={() => setRevision((v) => v + 1)}
      >
        {b && (
          <>
            <p className={styles.badge}>{t.statuses[b.status]}</p>
            <p>
              {b.trip.destination} · {b.trip.departure ?? b.trip.earliest} –{' '}
              {b.trip.return ?? b.trip.latest}
            </p>
            <p>
              {t.travelers}: {b.trip.adults + b.trip.children}
            </p>
            <p className={styles.notice}>{t.noBooking}</p>
            {b.reservations.map((r, i) => (
              <section className={styles.panel} key={i}>
                <h2>{r.title}</h2>
                <p className={styles.badge}>{t.statuses[r.status]}</p>
                {r.status === 'Confirmed' && r.confirmedDetails ? (
                  <>
                    <p>{t.confirmedReservation}</p>
                    <ResearchOptionSummary option={r.confirmedDetails} />
                    <p>
                      {t.supplierReference}: {r.supplierReference || '—'}
                    </p>
                    <p>
                      {t.confirmedAt}: {r.confirmedAt && date(r.confirmedAt)}
                    </p>
                    {r.ticketingStatus &&
                      r.ticketingStatus !== 'NotApplicable' && (
                        <p>
                          {r.ticketingStatus === 'Issued'
                            ? t.ticketIssued
                            : t.ticketPending}
                        </p>
                      )}
                  </>
                ) : (
                  <p>{t.pendingReservation}</p>
                )}
              </section>
            ))}
          </>
        )}
      </PortalLoad>
    </>
  );
}
export function CustomerDetailPage({ kind }: { kind: ResourceKind }) {
  const { id = '' } = useParams();
  return kind === 'requests' ? (
    <RequestPage key={id} id={id} />
  ) : kind === 'quotations' ? (
    <QuotationPage key={id} id={id} />
  ) : kind === 'invoices' ? (
    <InvoicePage key={id} id={id} />
  ) : kind === 'proposals' ? (
    <ProposalPage key={id} id={id} />
  ) : (
    <BookingPage key={id} id={id} />
  );
}
