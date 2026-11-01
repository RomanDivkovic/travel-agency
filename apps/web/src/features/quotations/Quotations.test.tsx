import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { LanguageProvider } from '../../localization/LanguageProvider';
import { ApiError } from '../../services/apiClient';
import { quotationApi, publicQuotationApi } from './api';
import { QuotationEditor } from './QuotationEditor';
import { CustomerQuotationPage } from './CustomerQuotationPage';
import { QuotationActions } from './QuotationActions';
import { calculatePreview, parseMinor } from './money';
import type {
  AdminQuotation,
  CustomerQuotation,
  QuotationConfiguration,
} from './types';
vi.mock('./api', () => ({
  quotationApi: {
    save: vi.fn(),
    send: vi.fn(),
    revise: vi.fn(),
    cancel: vi.fn(),
  },
  publicQuotationApi: { review: vi.fn(), respond: vi.fn() },
}));
const token = 'A'.repeat(43);
const quote: CustomerQuotation = {
  number: 'QUO-2026-000001',
  version: 1,
  status: 'Sent',
  language: 'sv',
  customerName: 'Test Customer',
  description: 'Travel service <script>untrusted</script>',
  includedServices: 'TEST scope',
  excludedServices: 'TEST exclusions',
  customerNotes: '',
  currency: 'SEK',
  taxTreatment: 'VatExclusive',
  subtotalMinorUnits: 5985,
  taxMinorUnits: 1496,
  totalMinorUnits: 7481,
  validUntil: '2027-06-01T12:00:00Z',
  termsSnapshot: 'TEST TERMS – not legal terms.',
  termsVersion: 'test-only-v1',
  acceptanceText: 'TEST ONLY: I confirm this service fee and terms.',
  acceptedAt: null,
  declinedAt: null,
  lines: [
    {
      description: 'TEST service',
      quantity: 3,
      unitPriceMinorUnits: 1995,
      taxRateBasisPoints: 2500,
      subtotalMinorUnits: 5985,
      taxMinorUnits: 1496,
      totalMinorUnits: 7481,
    },
  ],
};
const detail: AdminQuotation = {
  id: '12345678-1234-4234-8234-123456789abc',
  requestId: '12345678-1234-4234-8234-123456789aaa',
  requestReference: 'TRV-2026-000001',
  versionToken: '12345678-1234-4234-8234-123456789bbb',
  createdAt: '2026-11-01T10:00:00Z',
  updatedAt: '2026-11-01T10:00:00Z',
  sentAt: null,
  customerEmail: 'customer@example.test',
  isCurrentVersion: true,
  canRevise: false,
  quotation: { ...quote, status: 'Draft' },
};
const configuration: QuotationConfiguration = {
  issuanceReady: true,
  blockingReasons: [],
  approvedTaxTreatments: ['VatExclusive'],
  approvedTaxRatesBasisPoints: [2500],
  termsVersion: 'test-only-v1',
  termsSv: quote.termsSnapshot,
  termsEn: quote.termsSnapshot,
  defaultValidityDays: 14,
};
function mount(node: React.ReactNode, path = '/quote#' + token) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <LanguageProvider>{node}</LanguageProvider>
    </MemoryRouter>,
  );
}
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(publicQuotationApi.review).mockResolvedValue(quote);
  vi.mocked(quotationApi.save).mockResolvedValue(undefined);
});
describe('quotation pricing', () => {
  it.each([
    ['19.95', 1995],
    ['0.01', 1],
    ['100000000.00', 10000000000],
    ['19.999', undefined],
    ['-1', undefined],
    ['1e3', undefined],
    ['100000000.01', undefined],
  ])('parses minor units exactly: %s', (text, value) =>
    expect(parseMinor(text as string)).toBe(value),
  );
  it('previews exclusive/inclusive tax with per-line half-up rounding', () => {
    const lines = [
      {
        description: 'TEST',
        quantity: 3,
        unitPriceMinorUnits: 1995,
        taxRateBasisPoints: 2500,
      },
    ];
    expect(calculatePreview(lines, 'VatExclusive')).toEqual({
      subtotal: 5985,
      tax: 1496,
      total: 7481,
    });
    expect(calculatePreview(lines, 'VatIncluded')).toEqual({
      subtotal: 4788,
      tax: 1197,
      total: 5985,
    });
    expect(
      calculatePreview(
        [
          {
            ...lines[0]!,
            quantity: 1,
            unitPriceMinorUnits: 1,
            taxRateBasisPoints: 5000,
          },
        ],
        'VatExclusive',
      )?.tax,
    ).toBe(1);
    expect(calculatePreview(lines, 'Unconfirmed')).toBeUndefined();
  });
});
describe('quotation interfaces', () => {
  it('saves a validated draft without trusting preview totals', async () => {
    const saved = vi.fn();
    mount(
      <QuotationEditor
        detail={detail}
        configuration={configuration}
        onSaved={saved}
      />,
      '/admin/quotations/' + detail.id,
    );
    const u = userEvent.setup();
    const price = screen.getByLabelText('Styckpris 1');
    await u.clear(price);
    await u.type(price, '20.05');
    await u.click(screen.getByRole('button', { name: 'Spara utkast' }));
    await waitFor(() => expect(saved).toHaveBeenCalledTimes(1));
    expect(quotationApi.save).toHaveBeenCalledWith(
      detail.id,
      expect.objectContaining({
        versionToken: detail.versionToken,
        lines: [
          expect.objectContaining({
            unitPriceMinorUnits: 2005,
            quantity: 3,
            taxRateBasisPoints: 2500,
          }),
        ],
      }),
    );
    const payload = vi.mocked(quotationApi.save).mock.calls[0]![1];
    expect(payload).not.toHaveProperty('totalMinorUnits');
  });
  it('blocks issuing when approved business configuration is missing', () => {
    mount(
      <QuotationActions
        detail={detail}
        configuration={{ ...configuration, issuanceReady: false }}
        onSaved={vi.fn()}
      />,
    );
    expect(
      screen.getByRole('button', { name: 'Utfärda och köa e-post' }),
    ).toBeDisabled();
    expect(screen.getByText(/Utfärdande är spärrat/)).toBeVisible();
  });
  it('requires explicit send confirmation and then queues only once', async () => {
    vi.mocked(quotationApi.send).mockResolvedValue(undefined);
    const saved = vi.fn();
    mount(
      <QuotationActions
        detail={detail}
        configuration={configuration}
        onSaved={saved}
      />,
    );
    const u = userEvent.setup();
    await u.click(
      screen.getByRole('button', { name: 'Utfärda och köa e-post' }),
    );
    expect(quotationApi.send).not.toHaveBeenCalled();
    await u.click(screen.getByRole('button', { name: 'Ja, fortsätt' }));
    await waitFor(() => expect(saved).toHaveBeenCalledWith('send'));
    expect(quotationApi.send).toHaveBeenCalledTimes(1);
  });
  it('renders escaped content and leaves acceptance unchecked until explicitly selected', async () => {
    vi.mocked(publicQuotationApi.respond).mockResolvedValue({
      ...quote,
      status: 'Accepted',
      acceptedAt: '2026-11-01T11:00:00Z',
    });
    mount(<CustomerQuotationPage />);
    const u = userEvent.setup();
    await screen.findByText(quote.description);
    expect(document.querySelector('script')).toBeNull();
    const box = screen.getByRole('checkbox');
    expect(box).not.toBeChecked();
    expect(
      screen.getByRole('button', { name: 'Godkänn serviceofferten' }),
    ).toBeDisabled();
    await u.click(box);
    await u.click(
      screen.getByRole('button', { name: 'Godkänn serviceofferten' }),
    );
    expect(
      await screen.findByRole('heading', {
        name: 'Tack! Du har godkänt offerten.',
      }),
    ).toBeVisible();
    expect(
      screen.getByRole('heading', { name: 'Tack! Du har godkänt offerten.' }),
    ).toHaveFocus();
    expect(publicQuotationApi.respond).toHaveBeenCalledWith(token, true);
    expect(
      screen.queryByRole('button', { name: /betal/i }),
    ).not.toBeInTheDocument();
    expect(JSON.stringify(localStorage)).not.toContain(token);
    expect(JSON.stringify(sessionStorage)).not.toContain(token);
  });
  it('does not simulate acceptance after an uncertain failure and safely retries the same token', async () => {
    vi.mocked(publicQuotationApi.respond).mockRejectedValueOnce(
      new ApiError('network'),
    );
    mount(<CustomerQuotationPage />);
    const u = userEvent.setup();
    await screen.findByText(quote.description);
    await u.click(screen.getByRole('checkbox'));
    await u.click(
      screen.getByRole('button', { name: 'Godkänn serviceofferten' }),
    );
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Det gick inte att bekräfta',
    );
    expect(
      screen.queryByRole('heading', { name: 'Tack! Du har godkänt offerten.' }),
    ).not.toBeInTheDocument();
    vi.mocked(publicQuotationApi.respond).mockResolvedValue({
      ...quote,
      status: 'Accepted',
      acceptedAt: '2026-11-01T11:00:00Z',
    });
    await u.click(
      screen.getByRole('button', { name: 'Godkänn serviceofferten' }),
    );
    await screen.findByRole('heading', {
      name: 'Tack! Du har godkänt offerten.',
    });
    expect(
      vi.mocked(publicQuotationApi.respond).mock.calls.map((c) => c[0]),
    ).toEqual([token, token]);
  });
  it('declines only after confirmation and respects customer English language', async () => {
    vi.mocked(publicQuotationApi.review).mockResolvedValue({
      ...quote,
      language: 'en',
    });
    vi.mocked(publicQuotationApi.respond).mockResolvedValue({
      ...quote,
      language: 'en',
      status: 'Declined',
      declinedAt: '2026-11-01T11:00:00Z',
    });
    mount(<CustomerQuotationPage />);
    const u = userEvent.setup();
    await screen.findByRole('button', { name: 'Decline' });
    await u.click(screen.getByRole('button', { name: 'Decline' }));
    expect(publicQuotationApi.respond).not.toHaveBeenCalled();
    await u.click(screen.getByRole('button', { name: 'Yes, decline' }));
    expect(
      await screen.findByRole('heading', {
        name: 'You have declined the quotation.',
      }),
    ).toBeVisible();
    expect(publicQuotationApi.respond).toHaveBeenCalledWith(token, false);
  });
  it('never allows responding to an expired quotation', async () => {
    vi.mocked(publicQuotationApi.review).mockResolvedValue({
      ...quote,
      status: 'Expired',
    });
    mount(<CustomerQuotationPage />);
    expect(
      await screen.findByText(/Giltighetstiden har gått ut/),
    ).toBeVisible();
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Godkänn serviceofferten' }),
    ).not.toBeInTheDocument();
  });
  it('handles invalid or revoked tokens without showing quotation data', async () => {
    vi.mocked(publicQuotationApi.review).mockRejectedValue(
      new ApiError('Not found', 404),
    );
    mount(<CustomerQuotationPage />);
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Offertlänken är ogiltig',
    );
    expect(screen.queryByText(quote.customerName)).not.toBeInTheDocument();
  });
  it('disables duplicate responses while the first acceptance is pending', async () => {
    let resolve!: (q: CustomerQuotation) => void;
    vi.mocked(publicQuotationApi.respond).mockReturnValue(
      new Promise((r) => {
        resolve = r;
      }),
    );
    mount(<CustomerQuotationPage />);
    const u = userEvent.setup();
    await screen.findByText(quote.description);
    await u.click(screen.getByRole('checkbox'));
    await u.dblClick(
      screen.getByRole('button', { name: 'Godkänn serviceofferten' }),
    );
    expect(publicQuotationApi.respond).toHaveBeenCalledTimes(1);
    await act(async () =>
      resolve({
        ...quote,
        status: 'Accepted',
        acceptedAt: '2026-11-01T11:00:00Z',
      }),
    );
  });
});
