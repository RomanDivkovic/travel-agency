import { researchApi } from '../travel-research/api';
import { invoiceApi } from '../payments/api';
import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { App } from '../../app/App';
import { ApiError } from '../../services/apiClient';
import { adminApi } from './api';
import { quotationApi } from '../quotations/api';
import { safeAdminReturn, useAdminAuth } from './authContext';
import { AuthProvider } from './AuthProvider';
import type { AdminUser, RequestDetail, RequestItem } from './model';
vi.mock('./api', () => ({
  expiredEvent: 'travel-admin-session-ended',
  adminApi: {
    me: vi.fn(),
    login: vi.fn(),
    logout: vi.fn(),
    dashboard: vi.fn(),
    list: vi.fn(),
    detail: vi.fn(),
    notes: vi.fn(),
    activity: vi.fn(),
    status: vi.fn(),
    addNote: vi.fn(),
  },
}));
vi.mock('../travel-research/api', () => ({
  researchApi: { workspace: vi.fn() },
}));
vi.mock('../payments/api', () => ({
  invoiceApi: {
    summary: vi.fn().mockResolvedValue({ currencies: [], recentPayments: [] }),
  },
}));
vi.mock('../quotations/api', () => ({ quotationApi: { forRequest: vi.fn() } }));
const admin: AdminUser = {
  id: 'qa',
  displayName: 'QA Agent',
  email: 'qa@example.test',
  role: 'Administrator',
};
const id = '12345678-1234-4234-8234-123456789abc';
const item: RequestItem = {
  id,
  referenceNumber: 'TRV-2026-000123',
  customerName: 'Test Customer',
  serviceType: 'flights',
  destination: 'Lisbon',
  flexibleDestination: false,
  dateMode: 'fixed',
  departure: '2027-06-01',
  return: null,
  earliest: null,
  latest: null,
  createdAt: '2026-11-01T09:00:00Z',
  updatedAt: '2026-11-01T09:00:00Z',
  status: 'Submitted',
};
const detail: RequestDetail = {
  ...item,
  language: 'sv',
  contact: {
    name: 'Test Customer',
    email: 'customer@example.test',
    phone: null,
    preferredMethod: 'email',
  },
  plan: {
    origin: 'Stockholm',
    destination: 'Lisbon',
    flexibleDestination: false,
    additionalStops: [],
    dateMode: 'fixed',
    departure: '2027-06-01',
    return: null,
    earliest: null,
    latest: null,
    durationDays: null,
    adults: 2,
    childrenAges: [7],
    rooms: null,
    budgetAmount: 25000,
    currency: 'SEK',
    budgetScope: 'total',
    preferencesJson: JSON.stringify({
      flight: { priority: 'direct', baggage: 'cabin', flexibleAirports: false },
    }),
    activities: false,
    specialRequests: '<script>untrusted</script>',
  },
  version: '12345678-1234-4234-8234-123456789aaa',
  allowedStatuses: ['UnderReview', 'Closed'],
};
function mount(path = '/admin') {
  render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  );
}
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(researchApi.workspace).mockResolvedValue({
    eligible: false,
    status: 'NotStarted',
    versionToken: null,
    notes: '',
    options: [],
    proposals: {
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0,
    },
  });
  vi.mocked(invoiceApi.summary).mockResolvedValue({
    currencies: [],
    recentPayments: [],
  });
  vi.mocked(quotationApi.forRequest).mockResolvedValue({
    items: [],
    page: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
  });
  vi.mocked(adminApi.me).mockResolvedValue(admin);
  vi.mocked(adminApi.dashboard).mockResolvedValue({
    total: 7,
    submitted: 5,
    underReview: 1,
    awaitingCustomer: 1,
    closed: 0,
    recentlyUpdated: [item],
  });
  vi.mocked(adminApi.list).mockResolvedValue({
    items: [item],
    page: 1,
    pageSize: 20,
    totalCount: 21,
    totalPages: 2,
  });
  vi.mocked(adminApi.detail).mockResolvedValue(detail);
  vi.mocked(adminApi.notes).mockResolvedValue({
    items: [],
    page: 1,
    pageSize: 50,
    totalCount: 0,
    totalPages: 0,
  });
  vi.mocked(adminApi.activity).mockResolvedValue({
    items: [
      {
        id: 'event',
        type: 'Submitted',
        author: null,
        previousValue: null,
        newValue: null,
        createdAt: item.createdAt,
      },
    ],
    page: 1,
    pageSize: 50,
    totalCount: 1,
    totalPages: 1,
  });
});
describe('administration', () => {
  it('protects routes and redirects to login without exposing navigation', async () => {
    vi.mocked(adminApi.me).mockRejectedValue(new ApiError('Unauthorized', 401));
    mount('/admin/requests');
    expect(
      await screen.findByRole('heading', {
        name: 'Logga in till administrationen',
      }),
    ).toBeVisible();
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument();
    expect(adminApi.list).not.toHaveBeenCalled();
  });
  it('shows generic login failure, supports password visibility, and keeps passwords out of storage', async () => {
    vi.mocked(adminApi.me).mockRejectedValue(new ApiError('Unauthorized', 401));
    vi.mocked(adminApi.login).mockRejectedValue(
      new ApiError('Unauthorized', 401),
    );
    mount('/admin/login');
    const u = userEvent.setup();
    await u.type(screen.getByLabelText('E-postadress'), 'qa@example.test');
    await u.type(screen.getByLabelText('Lösenord'), 'WrongPassword!1');
    await u.click(screen.getByRole('button', { name: 'Visa lösenord' }));
    expect(screen.getByLabelText('Lösenord')).toHaveAttribute('type', 'text');
    await u.click(screen.getByRole('button', { name: 'Logga in' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'E-postadressen eller lösenordet är felaktig.',
    );
    expect(screen.getByLabelText('Lösenord')).toHaveValue('');
    expect(JSON.stringify(localStorage)).not.toContain('WrongPassword');
  });
  it('renders real dashboard data, changes language and logs out', async () => {
    vi.mocked(adminApi.logout).mockResolvedValue(undefined);
    mount();
    const u = userEvent.setup();
    expect(await screen.findByText('7')).toBeVisible();
    expect(screen.getByText('TRV-2026-000123')).toBeVisible();
    await u.selectOptions(
      screen.getByRole('combobox', { name: 'Språk' }),
      'en',
    );
    expect(screen.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
    await u.click(screen.getByRole('button', { name: 'Sign out' }));
    expect(
      await screen.findByRole('heading', { name: 'Sign in to administration' }),
    ).toBeVisible();
    expect(adminApi.logout).toHaveBeenCalledTimes(1);
  });
  it('sends filters and pagination to the API rather than filtering downloaded rows', async () => {
    mount('/admin/requests');
    const u = userEvent.setup();
    await screen.findByText('Test Customer');
    await u.type(
      screen.getByLabelText('Sök referens, namn eller e-post'),
      'customer@example.test',
    );
    await u.selectOptions(screen.getByLabelText('Status'), 'UnderReview');
    await u.click(screen.getByRole('button', { name: 'Sök och filtrera' }));
    await waitFor(() =>
      expect(adminApi.list).toHaveBeenLastCalledWith(
        expect.objectContaining({
          search: 'customer@example.test',
          status: 'UnderReview',
          page: 1,
        }),
        expect.any(AbortSignal),
      ),
    );
    await screen.findByText('Test Customer');
    await u.click(screen.getByRole('button', { name: 'Nästa' }));
    await waitFor(() =>
      expect(adminApi.list).toHaveBeenLastCalledWith(
        expect.objectContaining({ page: 2 }),
        expect.any(AbortSignal),
      ),
    );
    expect(location.search).toBe('');
  });
  it('renders escaped details, handles conflicts and adds private notes', async () => {
    mount('/admin/requests/' + id);
    const u = userEvent.setup();
    expect(await screen.findByText('<script>untrusted</script>')).toBeVisible();
    expect(document.querySelector('script')).toBeNull();
    vi.mocked(adminApi.status).mockRejectedValueOnce(
      new ApiError('Conflict', 409),
    );
    await u.click(screen.getByRole('button', { name: 'Spara status' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Förfrågan har ändrats',
    );
    expect(adminApi.status).toHaveBeenCalledWith(
      id,
      'UnderReview',
      detail.version,
    );
    vi.mocked(adminApi.addNote).mockResolvedValue(undefined);
    await u.type(screen.getByLabelText('Ny anteckning'), 'Private note');
    await u.click(screen.getByRole('button', { name: 'Lägg till anteckning' }));
    expect(await screen.findByRole('status')).toHaveTextContent(
      'Anteckningen har sparats.',
    );
    expect(adminApi.addNote).toHaveBeenCalledWith(id, 'Private note');
  });
  it('shows dashboard errors and successful retry', async () => {
    vi.mocked(adminApi.dashboard).mockRejectedValueOnce(
      new ApiError('Unavailable', 503),
    );
    mount();
    const u = userEvent.setup();
    expect(await screen.findByRole('alert')).toHaveTextContent('Det gick inte');
    await u.click(screen.getByRole('button', { name: 'Försök igen' }));
    expect(await screen.findByText('7')).toBeVisible();
  });
  it('does not allow external or unrelated login return URLs', () => {
    for (const value of [
      'https://evil.test',
      '//evil.test',
      '/admin/login',
      '/admin/requests/../../',
      '/',
    ])
      expect(safeAdminReturn(value)).toBe('/admin');
    expect(safeAdminReturn('/admin/requests/' + id)).toBe(
      '/admin/requests/' + id,
    );
  });
  it('removes protected data on an expired-session event', async () => {
    mount();
    await screen.findByText('Test Customer');
    act(() => window.dispatchEvent(new Event('travel-admin-session-ended')));
    expect(
      await screen.findByRole('heading', {
        name: 'Logga in till administrationen',
      }),
    ).toBeVisible();
    expect(screen.queryByText('Test Customer')).not.toBeInTheDocument();
  });
});

function SessionProbe() {
  const auth = useAdminAuth();
  return (
    <>
      <span>{auth.user?.displayName ?? 'Signed out'}</span>
      <button onClick={() => auth.setUser(null)}>End session</button>
    </>
  );
}
it('does not restore authentication from a stale session check after logout', async () => {
  let resolve!: (user: AdminUser) => void;
  vi.mocked(adminApi.me).mockReturnValue(
    new Promise<AdminUser>((r) => {
      resolve = r;
    }),
  );
  render(
    <AuthProvider>
      <SessionProbe />
    </AuthProvider>,
  );
  await userEvent
    .setup()
    .click(screen.getByRole('button', { name: 'End session' }));
  await act(async () => resolve(admin));
  expect(screen.getByText('Signed out')).toBeVisible();
  expect(screen.queryByText('QA Agent')).not.toBeInTheDocument();
});
