import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { LanguageProvider } from '../../localization/LanguageProvider';
import { ApiError } from '../../services/apiClient';
import { CustomerInvoicePage } from './CustomerInvoicePage';
import { AdminInvoiceListPage } from './AdminInvoiceListPage';
import { PaymentOverview } from './PaymentOverview';
import { SandboxPaymentPage } from './SandboxPaymentPage';
import { publicInvoiceApi, invoiceApi, safeCheckoutUrl } from './api';
import type { Invoice } from './types';
vi.mock('./api', async (original) => {
  const actual = await original<typeof import('./api')>();
  return {
    ...actual,
    publicInvoiceApi: {
      review: vi.fn(),
      session: vi.fn(),
      sandbox: vi.fn(),
      simulate: vi.fn(),
    },
    invoiceApi: { list: vi.fn(), summary: vi.fn() },
  };
});
const token = 'A'.repeat(43);
const invoice: Invoice = {
  number: 'TEST-INV-2026-1',
  quotationNumber: 'QUO-2026-1',
  quotationVersion: 2,
  status: 'Issued',
  isSimulated: true,
  language: 'sv',
  customerName: 'Test Customer',
  customerAddress: 'TEST address',
  description: 'TEST service <script>untrusted</script>',
  servicePeriod: 'TEST future service',
  currency: 'SEK',
  taxTreatment: 'VatExclusive',
  subtotalMinorUnits: 5985,
  taxMinorUnits: 1496,
  totalMinorUnits: 7481,
  lines: [
    {
      description: 'TEST planning',
      quantity: 3,
      unitPriceMinorUnits: 1995,
      taxRateBasisPoints: 2500,
      subtotalMinorUnits: 5985,
      taxMinorUnits: 1496,
      totalMinorUnits: 7481,
    },
  ],
  sellerName: 'TEST business',
  sellerAddress: 'TEST business address',
  registrationNumber: 'TEST ID',
  taxInformation: 'TEST tax',
  terms: 'TEST ONLY terms',
  termsVersion: 'TEST-v1',
  dueAt: '2027-06-01T12:00:00Z',
  issuedAt: '2026-11-01T12:00:00Z',
  paidAt: null,
};
function mount(node: React.ReactNode, path = '/invoice#' + token) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <LanguageProvider>{node}</LanguageProvider>
    </MemoryRouter>,
  );
}
beforeEach(() => {
  vi.clearAllMocks();
  localStorage.clear();
  vi.mocked(publicInvoiceApi.review).mockResolvedValue(invoice);
});
describe('customer invoices', () => {
  it('renders exact snapshots, escaped content, service scope and simulation disclosure', async () => {
    mount(<CustomerInvoicePage />);
    expect(
      await screen.findByRole('heading', { name: 'Betala din serviceavgift' }),
    ).toBeInTheDocument();
    expect(await screen.findByText(invoice.description)).toBeInTheDocument();
    expect(screen.getByText('TEST-INV-2026-1')).toBeInTheDocument();
    expect(screen.getByText(/inga pengar dras/)).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Öppna simulerad testbetalning' }),
    ).toBeInTheDocument();
    expect(document.querySelector('script')).toBeNull();
    expect(localStorage.getItem('token')).toBeNull();
    expect(document.querySelector('meta[name="robots"]')).toHaveAttribute(
      'content',
      'noindex, nofollow',
    );
  });
  it('shows loading and never claims success from return query parameters', async () => {
    vi.mocked(publicInvoiceApi.review).mockImplementation(
      () => new Promise(() => {}),
    );
    mount(
      <CustomerInvoicePage result />,
      '/payment/result?success=true#' + token,
    );
    expect(screen.getByRole('status')).toHaveTextContent('Hämtar');
    expect(screen.queryByText(/Din betalning är registrerad/)).toBeNull();
  });
  it('shows pending status from the backend despite a success redirect', async () => {
    mount(
      <CustomerInvoicePage result />,
      '/payment/result?success=true#' + token,
    );
    await screen.findByText(invoice.description);
    expect(
      screen.getByRole('heading', {
        name: 'Vi inväntar betalningsbekräftelse.',
      }),
    ).toBeInTheDocument();
    expect(screen.queryByText(/Din betalning är registrerad/)).toBeNull();
  });
  it('renders verified real payment only from backend state', async () => {
    vi.mocked(publicInvoiceApi.review).mockResolvedValue({
      ...invoice,
      status: 'Paid',
      isSimulated: false,
      paidAt: '2026-11-02T12:00:00Z',
    });
    mount(<CustomerInvoicePage result />);
    expect(
      await screen.findByRole('heading', {
        name: 'Tack! Din betalning är registrerad.',
      }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /säker betalning/ }),
    ).toBeNull();
  });
  it('keeps simulated success explicitly separate from real payment', async () => {
    vi.mocked(publicInvoiceApi.review).mockResolvedValue({
      ...invoice,
      status: 'Paid',
    });
    mount(<CustomerInvoicePage result />);
    expect(
      await screen.findByText(/Endast testbehörighet/),
    ).toBeInTheDocument();
    expect(
      screen.queryByText('Tack! Din betalning är registrerad.'),
    ).toBeNull();
  });
  it('shows failed attempts and a safe retry action without trusting the browser', async () => {
    vi.mocked(publicInvoiceApi.review).mockResolvedValue({
      ...invoice,
      lastPaymentStatus: 'Failed',
      language: 'en',
    });
    vi.mocked(publicInvoiceApi.session).mockRejectedValue(
      new ApiError('outage', 503),
    );
    mount(<CustomerInvoicePage result />);
    await screen.findByRole('heading', {
      name: 'Payment could not be completed.',
    });
    await userEvent.click(
      screen.getByRole('button', { name: 'Open simulated test payment' }),
    );
    expect(publicInvoiceApi.session).toHaveBeenCalledWith(token);
    await screen.findByRole('alert');
    expect(
      screen.getByRole('button', { name: 'Open simulated test payment' }),
    ).toBeEnabled();
  });
  it('blocks duplicate payment clicks while a session is being created', async () => {
    vi.mocked(publicInvoiceApi.session).mockImplementation(
      () => new Promise(() => {}),
    );
    mount(<CustomerInvoicePage />);
    const b = await screen.findByRole('button', {
      name: 'Öppna simulerad testbetalning',
    });
    await userEvent.dblClick(b);
    expect(publicInvoiceApi.session).toHaveBeenCalledTimes(1);
    expect(screen.getByRole('button', { name: 'Arbetar…' })).toBeDisabled();
  });
  it('rejects invalid tokens without making an API request', async () => {
    mount(<CustomerInvoicePage />, '/invoice#not-a-token');
    expect(await screen.findByRole('alert')).toHaveTextContent('ogiltig');
    expect(publicInvoiceApi.review).not.toHaveBeenCalled();
  });
  it('supports language switching without altering snapshot text', async () => {
    mount(<CustomerInvoicePage />);
    await screen.findByText(invoice.description);
    await userEvent.selectOptions(
      screen.getByRole('combobox', { name: 'Språk' }),
      'en',
    );
    expect(
      screen.getByRole('heading', { name: 'Pay your service fee' }),
    ).toBeInTheDocument();
    expect(screen.getByText(invoice.terms)).toBeInTheDocument();
  });
  it('recovers from uncertain reads by reloading authoritative state', async () => {
    vi.mocked(publicInvoiceApi.review).mockRejectedValueOnce(
      new ApiError('network'),
    );
    mount(<CustomerInvoicePage />);
    await screen.findByRole('alert');
    await userEvent.click(
      screen.getByRole('button', { name: 'Hämta aktuell status' }),
    );
    await screen.findByText(invoice.description);
    expect(publicInvoiceApi.review).toHaveBeenCalledTimes(2);
  });
});
describe('administration and sandbox', () => {
  it('uses server pagination and filters', async () => {
    vi.mocked(invoiceApi.list).mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 20,
      total: 21,
    });
    mount(<AdminInvoiceListPage />);
    await screen.findByText('Inga fakturor hittades.');
    await userEvent.click(screen.getByRole('button', { name: 'Nästa' }));
    await waitFor(() =>
      expect(invoiceApi.list).toHaveBeenLastCalledWith(
        2,
        '',
        '',
        expect.any(AbortSignal),
      ),
    );
    await userEvent.selectOptions(screen.getByRole('combobox'), 'Paid');
    await waitFor(() =>
      expect(invoiceApi.list).toHaveBeenLastCalledWith(
        1,
        'Paid',
        '',
        expect.any(AbortSignal),
      ),
    );
  });
  it('keeps currencies and simulation metrics separate', async () => {
    vi.mocked(invoiceApi.summary).mockResolvedValue({
      currencies: [
        {
          currency: 'SEK',
          isSimulated: false,
          paidMinorUnits: 10000,
          outstandingMinorUnits: 0,
          pendingPayments: 0,
          overdueInvoices: 0,
        },
        {
          currency: 'EUR',
          isSimulated: true,
          paidMinorUnits: 5000,
          outstandingMinorUnits: 1000,
          pendingPayments: 1,
          overdueInvoices: 0,
        },
      ],
      recentPayments: [],
    });
    mount(<PaymentOverview />);
    await screen.findByRole('heading', { name: 'EUR' });
    expect(screen.getByRole('heading', { name: 'SEK' })).toBeInTheDocument();
    expect(screen.getByText(/inga pengar dras/)).toBeInTheDocument();
  });
  it('explicitly simulates a failure without claiming a real charge', async () => {
    vi.mocked(publicInvoiceApi.sandbox).mockResolvedValue({
      amountMinorUnits: 1000,
      currency: 'SEK',
      status: 'Pending',
      expiresAt: '2027-01-01T12:00:00Z',
      isSimulated: true,
      resultUrl: window.location.origin + '/payment/result#' + token,
    });
    vi.mocked(publicInvoiceApi.simulate).mockResolvedValue(undefined);
    mount(<SandboxPaymentPage />, '/payment/sandbox#' + token);
    await userEvent.click(
      await screen.findByRole('button', {
        name: 'Simulera misslyckad betalning',
      }),
    );
    await screen.findByRole('status');
    expect(publicInvoiceApi.simulate).toHaveBeenCalledWith(token, 'Failed');
    expect(
      screen.getByRole('link', { name: 'Betalningsstatus' }),
    ).toHaveAttribute('href', '/payment/result#' + token);
  });
  it('restricts simulated checkout to the exact same-origin simulator route', () => {
    expect(
      safeCheckoutUrl(
        window.location.origin + '/payment/sandbox#' + token,
        true,
      ),
    ).toContain('/payment/sandbox#');
    expect(() =>
      safeCheckoutUrl('https://evil.example/payment/sandbox', true),
    ).toThrow();
    expect(() => safeCheckoutUrl('javascript:alert(1)', false)).toThrow();
    expect(() =>
      safeCheckoutUrl(window.location.origin + '/other', true),
    ).toThrow();
  });
});
