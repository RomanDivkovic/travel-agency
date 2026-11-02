import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router';
import { beforeEach, it, expect, vi } from 'vitest';
import { LanguageProvider } from '../../localization/LanguageProvider';
import { ApiError } from '../../services/apiClient';
import { bookingApi } from './api';
import { EvidenceForm, ConfirmAction } from './BookingShared';
import { AdminBookingDetailPage } from './BookingWorkspace';
import { AdminBookingListPage } from './BookingList';
import { ReservationEditor } from './ReservationEditor';
import { ReservationActions } from './ReservationActions';
import type { Booking, Reservation } from './types';
vi.mock('./api', () => ({
  bookingApi: {
    detail: vi.fn(),
    history: vi.fn(),
    list: vi.fn(),
    reservation: vi.fn(),
    send: vi.fn(),
    notes: vi.fn(),
  },
}));
const snapshot = {
  kind: 'Extra' as const,
  title: 'TEST transfer',
  description: 'TEST details',
  priceMinorUnits: 10000,
  totalMinorUnits: 10000,
  currency: 'SEK',
  priceBasis: 'Total',
  quantity: 1,
  supplierName: 'TEST supplier',
  supplierUrl: 'https://supplier.example.test/transfer',
  priceCheckedAt: '2026-11-02T12:00:00Z',
  availabilityCheckedAt: '2026-11-02T12:00:00Z',
  conditions: 'TEST conditions',
  flight: null,
  accommodation: null,
  extra: { category: 'Transfer', location: 'Lisbon', date: '2027-06-01' },
};
const reservation: Reservation = {
  id: 'reservation-id',
  proposalItemId: 'item-id',
  reservationType: 'Transfer',
  status: 'NotStarted',
  approved: snapshot,
  current: snapshot,
  requiresReview: false,
  reapprovalRecorded: false,
  arrangementVersion: 1,
  paymentResponsibility: null,
  paymentStatus: 'Unknown',
  paymentArrangement: '',
  supplierReference: '',
  confirmationEvidence: '',
  documentReference: '',
  ticketingStatus: 'NotApplicable',
  failureReason: '',
  cancellationReason: '',
  cancellationReference: '',
  cancellationChargeMinorUnits: null,
  confirmedAt: null,
  cancelledAt: null,
};
const detail: Booking = {
  booking: {
    id: 'booking-id',
    requestId: 'request-id',
    proposalId: 'proposal-id',
    reference: 'BKG-TEST',
    customerName: 'TEST customer',
    destination: 'Lisbon',
    departure: '2027-06-01',
    return: '2027-06-07',
    status: 'PartiallyConfirmed',
    confirmed: 1,
    pending: 1,
    failed: 0,
    updatedAt: '2026-11-02T12:00:00Z',
  },
  versionToken: 'version-token',
  revision: 1,
  notes: 'TEST private',
  eligible: true,
  reservations: [reservation],
  notifications: [],
  proposal: {
    number: 'PROP-TEST',
    version: 1,
    status: 'Approved',
    language: 'en',
    title: 'TEST travel',
    introduction: 'TEST introduction',
    summary: 'TEST summary',
    termsSnapshot: 'TEST terms',
    termsVersion: 'TEST-v1',
    acceptanceText: 'TEST consent',
    trip: {
      origin: 'ARN',
      destination: 'Lisbon',
      flexibleDestination: false,
      additionalStops: [],
      dateMode: 'fixed',
      departure: '2027-06-01',
      return: '2027-06-07',
      earliest: null,
      latest: null,
      durationDays: null,
      adults: 2,
      children: 0,
      rooms: 1,
    },
    paidServiceFee: {
      invoiceNumber: 'INV-TEST',
      currency: 'SEK',
      amountMinorUnits: 25000,
      paidAt: '2026-11-01T10:00:00Z',
    },
    validUntil: '2026-11-10T12:00:00Z',
    approvedAt: '2026-11-02T10:00:00Z',
    rejectedAt: null,
    selectedAlternativeId: 'alt',
    canRespond: false,
    alternatives: [
      {
        id: 'alt',
        title: 'TEST selected alternative',
        description: 'TEST',
        recommended: false,
        items: [snapshot],
        estimatedCosts: [
          {
            currency: 'SEK',
            flightsMinorUnits: 0,
            accommodationMinorUnits: 0,
            extrasMinorUnits: 10000,
            totalMinorUnits: 10000,
          },
        ],
      },
    ],
  },
};
function show(node: React.ReactNode) {
  return render(
    <LanguageProvider>
      <MemoryRouter initialEntries={['/admin/bookings/booking-id']}>
        <Routes>
          <Route path="/admin/bookings/:id" element={node} />
        </Routes>
      </MemoryRouter>
    </LanguageProvider>,
  );
}
beforeEach(() => {
  vi.clearAllMocks();
  localStorage.setItem('travel-agency.language.v1', 'en');
  vi.mocked(bookingApi.detail).mockResolvedValue(structuredClone(detail));
  vi.mocked(bookingApi.history).mockResolvedValue({
    items: [],
    page: 1,
    pageSize: 50,
    totalCount: 0,
    totalPages: 0,
  });
  vi.mocked(bookingApi.list).mockResolvedValue({
    items: [detail.booking],
    page: 1,
    pageSize: 20,
    totalCount: 1,
    totalPages: 1,
  });
});
it('requires explicit unchecked authority and submits the exact evidence', async () => {
  const user = userEvent.setup();
  const save = vi.fn().mockResolvedValue(undefined);
  show(<EvidenceForm onSave={save} onClose={() => {}} />);
  expect(screen.getByRole('checkbox')).not.toBeChecked();
  expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled();
  await user.type(screen.getByLabelText('Customer name'), 'TEST customer');
  await user.type(
    screen.getByLabelText('Approval reference / evidence'),
    'TEST email 123',
  );
  await user.type(
    screen.getByLabelText('Customer approved at (UTC)'),
    '2026-11-02T12:00',
  );
  await user.click(screen.getByRole('checkbox'));
  await user.click(screen.getByRole('button', { name: 'Save' }));
  await waitFor(() =>
    expect(save).toHaveBeenCalledWith(
      expect.objectContaining({
        confirmed: true,
        approvedAt: '2026-11-02T12:00:00.000Z',
        reference: 'TEST email 123',
      }),
    ),
  );
});
it('does not claim evidence saved when the server rejects it', async () => {
  const user = userEvent.setup();
  show(
    <ConfirmAction
      label="TEST operation"
      hint="TEST confirm"
      onConfirm={() => Promise.reject(new ApiError('Conflict', 409))}
    />,
  );
  await user.click(screen.getByRole('button', { name: 'TEST operation' }));
  await user.click(screen.getByRole('button', { name: 'Confirm this action' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('changed');
  expect(screen.queryByText('Saved by the server.')).not.toBeInTheDocument();
});
it('shows partial status and blocks full booking confirmation', async () => {
  show(<AdminBookingDetailPage />);
  expect(
    await screen.findByRole('heading', { name: 'BKG-TEST' }),
  ).toBeVisible();
  expect(screen.getByText('Partially confirmed')).toBeVisible();
  expect(
    screen.queryByRole('button', { name: 'Queue full booking confirmation' }),
  ).not.toBeInTheDocument();
  expect(
    screen.getByRole('heading', { name: 'Booking checklist' }),
  ).toBeVisible();
});
it('queues only a confirmed trip after explicit confirmation', async () => {
  vi.mocked(bookingApi.detail).mockResolvedValue({
    ...detail,
    booking: {
      ...detail.booking,
      status: 'Confirmed',
      confirmed: 1,
      pending: 0,
    },
    reservations: [{ ...reservation, status: 'Confirmed' }],
  });
  vi.mocked(bookingApi.send).mockResolvedValue(undefined);
  const user = userEvent.setup();
  show(<AdminBookingDetailPage />);
  await user.click(
    await screen.findByRole('button', {
      name: 'Queue full booking confirmation',
    }),
  );
  expect(bookingApi.send).not.toHaveBeenCalled();
  await user.click(screen.getByRole('button', { name: 'Confirm this action' }));
  await waitFor(() =>
    expect(bookingApi.send).toHaveBeenCalledWith('booking-id', 'version-token'),
  );
  expect(await screen.findByRole('status')).toHaveTextContent(
    'Saved by the server.',
  );
});
it('forwards search and sorting to the paginated API', async () => {
  const user = userEvent.setup();
  show(<AdminBookingListPage />);
  await screen.findByRole('link', { name: 'BKG-TEST' });
  await user.type(
    screen.getByLabelText('Search reference or customer'),
    'BKG-TEST',
  );
  await user.click(
    screen.getByRole('button', { name: 'Search reference or customer' }),
  );
  await user.selectOptions(screen.getByLabelText('Sort'), 'departure');
  await waitFor(() =>
    expect(bookingApi.list).toHaveBeenLastCalledWith(
      expect.any(URLSearchParams),
      expect.any(AbortSignal),
    ),
  );
  const query = vi.mocked(bookingApi.list).mock.calls.at(-1)![0];
  expect(query.get('search')).toBe('BKG-TEST');
  expect(query.get('sort')).toBe('departure');
});
it('requires payment responsibility and persists changed price in exact minor units', async () => {
  const user = userEvent.setup();
  const saved = vi.fn();
  vi.mocked(bookingApi.reservation).mockResolvedValue(undefined);
  show(
    <ReservationEditor
      detail={detail}
      r={reservation}
      onSaved={saved}
      onClose={() => {}}
    />,
  );
  expect(
    screen.queryByLabelText('Private option notes'),
  ).not.toBeInTheDocument();
  await user.selectOptions(
    screen.getByLabelText('Payment responsibility'),
    'CustomerPaysSupplier',
  );
  await user.type(
    screen.getByLabelText(
      'Agreed payment arrangement / authorization reference',
    ),
    'TEST approved direct payment',
  );
  const price = screen.getByLabelText('Price per stated unit');
  await user.clear(price);
  await user.type(price, '123.45');
  await user.click(screen.getByRole('button', { name: 'Save option' }));
  await waitFor(() => expect(saved).toHaveBeenCalled());
  expect(bookingApi.reservation).toHaveBeenCalledWith(
    'booking-id',
    'reservation-id',
    'version-token',
    'update',
    expect.objectContaining({
      option: expect.objectContaining({ priceMinorUnits: 12345 }),
      paymentResponsibility: 'CustomerPaysSupplier',
    }),
  );
});
it('offers snapshot-specific reapproval and records supplier failures without fake success', async () => {
  const user = userEvent.setup();
  show(
    <ReservationActions
      detail={detail}
      r={{ ...reservation, status: 'InProgress', requiresReview: true }}
      onSaved={() => {}}
    />,
  );
  expect(
    screen.getByRole('button', { name: 'Record customer reapproval' }),
  ).toBeVisible();
  await user.click(screen.getByRole('button', { name: 'Record failure' }));
  await user.type(screen.getByLabelText('Reason'), 'TEST unavailable');
  expect(screen.getByRole('checkbox')).not.toBeChecked();
  await user.click(screen.getByRole('checkbox'));
  await user.click(screen.getByRole('button', { name: 'Save' }));
  await waitFor(() =>
    expect(bookingApi.reservation).toHaveBeenCalledWith(
      'booking-id',
      'reservation-id',
      'version-token',
      'fail',
      { reason: 'TEST unavailable' },
    ),
  );
});
it('renders the booking workspace in Swedish by default', async () => {
  localStorage.removeItem('travel-agency.language.v1');
  show(<AdminBookingDetailPage />);
  expect(
    await screen.findByRole('heading', { name: 'Bokningsarbetsyta' }),
  ).toBeVisible();
  expect(screen.getByText('Delvis bekräftad')).toBeVisible();
});

it('moves keyboard focus into confirmation and restores it on cancel', async () => {
  const user = userEvent.setup();
  show(
    <ConfirmAction
      label="TEST keyboard action"
      hint="TEST review"
      onConfirm={async () => {}}
    />,
  );
  const opener = screen.getByRole('button', { name: 'TEST keyboard action' });
  opener.focus();
  await user.keyboard('{Enter}');
  expect(
    screen.getByRole('button', { name: 'Confirm this action' }),
  ).toHaveFocus();
  await user.tab();
  expect(screen.getByRole('button', { name: 'Close' })).toHaveFocus();
  await user.keyboard('{Enter}');
  expect(
    opener.isConnected
      ? opener
      : screen.getByRole('button', { name: 'TEST keyboard action' }),
  ).toHaveFocus();
});
