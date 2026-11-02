import { AdminProposalPage } from './AdminProposalPage';
import { render, screen, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, it, expect, vi } from 'vitest';
import { LanguageProvider } from '../../localization/LanguageProvider';
import { ApiError } from '../../services/apiClient';
import { CustomerProposalPage } from './CustomerProposalPage';
import { ResearchOptionEditor } from './ResearchOptionEditor';
import { ResearchOptions } from './ResearchOptions';
import { ProposalEditor } from './ProposalEditor';
import { ProposalSummary } from './ProposalSummary';
import { proposalApi, publicProposalApi, researchApi } from './api';
import { reorder, flightMetrics } from './formHelpers';
import type { AdminProposal, CustomerProposal, ResearchOption } from './types';
vi.mock('./api', () => ({
  proposalApi: {
    save: vi.fn(),
    detail: vi.fn(),
    configuration: vi.fn(),
    history: vi.fn(),
  },
  publicProposalApi: { review: vi.fn(), respond: vi.fn() },
  researchApi: {
    saveOption: vi.fn(),
    removeOption: vi.fn(),
    workspace: vi.fn(),
  },
}));
const token = 'A'.repeat(43);
const item: ResearchOption = {
  id: '11111111-1111-4111-8111-111111111111',
  versionToken: '22222222-2222-4222-8222-222222222222',
  notes: 'SECRET PRIVATE NOTE',
  updatedAt: '2026-11-01T10:00:00Z',
  option: {
    kind: 'Accommodation',
    title: 'TEST hotel <script>untrusted</script>',
    description: 'TEST description',
    priceMinorUnits: 10000,
    totalMinorUnits: 60000,
    currency: 'SEK',
    priceBasis: 'PerNight',
    quantity: 6,
    supplierName: 'TEST supplier',
    supplierUrl: 'https://supplier.example.test/hotel',
    priceCheckedAt: '2026-11-01T10:00:00Z',
    availabilityCheckedAt: null,
    conditions: 'TEST cancellation terms',
    flight: null,
    extra: null,
    accommodation: {
      destination: 'Lisbon',
      address: 'TEST address',
      accommodationType: 'Hotel',
      roomType: 'Double',
      checkIn: '2027-06-01',
      checkOut: '2027-06-07',
      guests: 2,
      mealPlan: 'Breakfast',
      amenities: 'Pool',
    },
  },
};
const proposal: CustomerProposal = {
  number: 'PROP-2026-1',
  version: 1,
  status: 'Sent',
  language: 'sv',
  title: 'TEST trip',
  introduction: 'TEST introduction',
  summary: 'TEST travel summary',
  termsSnapshot: 'TEST ONLY — not legal terms.',
  termsVersion: 'TEST-v1',
  acceptanceText:
    'TEST explicit acknowledgement of this selection and estimated costs.',
  trip: {
    origin: 'Stockholm',
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
    invoiceNumber: 'TEST-INV-1',
    currency: 'SEK',
    amountMinorUnits: 10000,
    paidAt: '2026-11-01T10:00:00Z',
  },
  validUntil: '2027-05-01T12:00:00Z',
  approvedAt: null,
  rejectedAt: null,
  selectedAlternativeId: null,
  canRespond: true,
  alternatives: [
    {
      id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
      title: 'Option A',
      description: 'TEST budget',
      recommended: false,
      items: [item.option],
      estimatedCosts: [
        {
          currency: 'SEK',
          flightsMinorUnits: 0,
          accommodationMinorUnits: 60000,
          extrasMinorUnits: 0,
          totalMinorUnits: 60000,
        },
      ],
    },
    {
      id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
      title: 'Option B',
      description: 'TEST different currencies',
      recommended: true,
      items: [{ ...item.option, currency: 'EUR' }],
      estimatedCosts: [
        {
          currency: 'EUR',
          flightsMinorUnits: 0,
          accommodationMinorUnits: 60000,
          extrasMinorUnits: 0,
          totalMinorUnits: 60000,
        },
      ],
    },
  ],
};
const detail: AdminProposal = {
  id: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
  requestId: 'dddddddd-dddd-4ddd-8ddd-dddddddddddd',
  versionToken: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
  customerEmail: 'test@example.test',
  isCurrentVersion: true,
  canRevise: false,
  proposal: { ...proposal, status: 'Draft', canRespond: false },
  itemIds: [[item.id], [item.id]],
};
function mount(node: React.ReactNode, path = '/travel-proposal#' + token) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <LanguageProvider>{node}</LanguageProvider>
    </MemoryRouter>,
  );
}
beforeEach(() => {
  vi.resetAllMocks();
  localStorage.clear();
  vi.mocked(publicProposalApi.review).mockResolvedValue(proposal);
  vi.mocked(researchApi.saveOption).mockResolvedValue(undefined);
  vi.mocked(proposalApi.save).mockResolvedValue(undefined);
});
describe('secure customer proposals', () => {
  it('requires explicit alternative and unchecked acknowledgement before confirmation', async () => {
    mount(<CustomerProposalPage />);
    await screen.findByText('TEST trip');
    expect(
      screen
        .getAllByRole('radio')
        .every((r) => !(r as HTMLInputElement).checked),
    ).toBe(true);
    expect(screen.getByRole('checkbox')).not.toBeChecked();
    const approve = screen.getByRole('button', {
      name: 'Godkänn valt resealternativ',
    });
    expect(approve).toBeDisabled();
    await userEvent.click(screen.getByRole('radio', { name: 'Option B' }));
    expect(approve).toBeDisabled();
    await userEvent.click(screen.getByRole('checkbox'));
    await userEvent.click(approve);
    expect(publicProposalApi.respond).not.toHaveBeenCalled();
    await screen.findByRole('heading', {
      name: 'Bekräfta ditt val och de uppskattade leverantörskostnaderna',
    });
  });
  it('approves only the chosen option after backend confirmation and focuses feedback', async () => {
    vi.mocked(publicProposalApi.respond).mockResolvedValue({
      ...proposal,
      status: 'Approved',
      canRespond: false,
      selectedAlternativeId: proposal.alternatives[1]!.id,
    });
    mount(<CustomerProposalPage />);
    await screen.findByText('TEST trip');
    await userEvent.click(screen.getByRole('radio', { name: 'Option B' }));
    await userEvent.click(screen.getByRole('checkbox'));
    await userEvent.click(
      screen.getByRole('button', { name: 'Godkänn valt resealternativ' }),
    );
    await userEvent.click(screen.getByRole('button', { name: 'Ja, bekräfta' }));
    const heading = await screen.findByRole('heading', {
      name: 'Tack! Ditt val har registrerats.',
    });
    expect(heading).toHaveFocus();
    expect(publicProposalApi.respond).toHaveBeenCalledWith(
      token,
      proposal,
      true,
      proposal.alternatives[1]!.id,
      '',
    );
    expect(screen.queryByRole('radio')).toBeNull();
  });
  it('rejects with optional feedback and no prechecked selection', async () => {
    vi.mocked(publicProposalApi.respond).mockResolvedValue({
      ...proposal,
      status: 'Rejected',
      canRespond: false,
    });
    mount(<CustomerProposalPage />);
    await screen.findByText('TEST trip');
    await userEvent.type(
      screen.getByRole('textbox', { name: 'Återkoppling (valfritt)' }),
      'TEST feedback',
    );
    await userEvent.click(
      screen.getByRole('button', { name: 'Avböj reseförslaget' }),
    );
    await userEvent.click(screen.getByRole('button', { name: 'Ja, bekräfta' }));
    await screen.findByRole('heading', {
      name: 'Du har avböjt reseförslaget.',
    });
    expect(publicProposalApi.respond).toHaveBeenCalledWith(
      token,
      proposal,
      false,
      null,
      'TEST feedback',
    );
  });
  it('does not claim success on uncertain errors and reloads safely', async () => {
    vi.mocked(publicProposalApi.respond).mockRejectedValue(
      new ApiError('network'),
    );
    mount(<CustomerProposalPage />);
    await screen.findByText('TEST trip');
    await userEvent.click(
      screen.getByRole('button', { name: 'Avböj reseförslaget' }),
    );
    await userEvent.click(screen.getByRole('button', { name: 'Ja, bekräfta' }));
    await screen.findByRole('alert');
    expect(screen.queryByText('Du har avböjt reseförslaget.')).toBeNull();
    await userEvent.click(
      screen.getByRole('button', { name: 'Hämta aktuell status' }),
    );
    await waitFor(() =>
      expect(publicProposalApi.review).toHaveBeenCalledTimes(2),
    );
  });
  it('prevents duplicate decisions while waiting for the server', async () => {
    let resolve!: (p: CustomerProposal) => void;
    vi.mocked(publicProposalApi.respond).mockImplementation(
      () =>
        new Promise((r) => {
          resolve = r;
        }),
    );
    mount(<CustomerProposalPage />);
    await screen.findByText('TEST trip');
    await userEvent.click(
      screen.getByRole('button', { name: 'Avböj reseförslaget' }),
    );
    await userEvent.dblClick(
      screen.getByRole('button', { name: 'Ja, bekräfta' }),
    );
    expect(publicProposalApi.respond).toHaveBeenCalledTimes(1);
    await act(() =>
      resolve({ ...proposal, status: 'Rejected', canRespond: false }),
    );
  });
  it('renders expired and unavailable links without actions', async () => {
    vi.mocked(publicProposalApi.review).mockResolvedValue({
      ...proposal,
      status: 'Expired',
      canRespond: false,
    });
    mount(<CustomerProposalPage />);
    await screen.findByText(
      'Giltighetstiden har gått ut. Denna version kan inte godkännas.',
    );
    expect(screen.queryByRole('checkbox')).toBeNull();
  });
  it('rejects malformed token before API access', async () => {
    mount(<CustomerProposalPage />, '/travel-proposal#bad-token');
    await screen.findByRole('alert');
    expect(publicProposalApi.review).not.toHaveBeenCalled();
  });
  it('keeps internal notes out, escapes supplier content, separates service fee and currencies', async () => {
    mount(<CustomerProposalPage />);
    await screen.findByText('TEST trip');
    expect(screen.getAllByText(item.option.title)).toHaveLength(2);
    expect(screen.queryByText('SECRET PRIVATE NOTE')).toBeNull();
    expect(document.querySelector('script')).toBeNull();
    expect(
      screen.getByText(
        'Redan betald — ingår inte i leverantörssumman och tas inte ut igen.',
      ),
    ).toBeInTheDocument();
    expect(
      screen.getAllByRole('link', { name: 'TEST supplier' })[0],
    ).toHaveAttribute('rel', 'noopener noreferrer');
    expect(document.querySelector('meta[name="robots"]')).toHaveAttribute(
      'content',
      'noindex, nofollow',
    );
    expect(document.querySelector('link[rel="canonical"]')).toBeNull();
  });
  it('switches labels to English without changing legal snapshots', async () => {
    mount(<CustomerProposalPage />);
    await screen.findByText('TEST trip');
    await userEvent.selectOptions(
      screen.getByRole('combobox', { name: 'Språk' }),
      'en',
    );
    expect(
      screen.getByRole('heading', {
        name: 'Your personalized travel proposal',
      }),
    ).toBeInTheDocument();
    expect(screen.getByText(proposal.termsSnapshot)).toBeInTheDocument();
  });
  it('keeps trailing-slash proposal URLs private when a public origin is configured', async () => {
    vi.stubEnv('VITE_SITE_URL', 'https://travel.example.test');
    try {
      mount(<CustomerProposalPage />, '/travel-proposal/#' + token);
      await screen.findByText('TEST trip');
      expect(document.querySelector('link[rel="canonical"]')).toBeNull();
      expect(document.querySelector('meta[name="robots"]')).toHaveAttribute(
        'content',
        'noindex, nofollow',
      );
    } finally {
      vi.unstubAllEnvs();
    }
  });
});
describe('research editors and proposal builder', () => {
  it('saves exact minor units and private notes through the dedicated API', async () => {
    const saved = vi.fn();
    mount(
      <ResearchOptionEditor
        requestId={detail.requestId}
        kind="Accommodation"
        option={item}
        onSaved={saved}
        onClose={vi.fn()}
      />,
    );
    await userEvent.clear(
      screen.getByRole('textbox', { name: 'Pris per angiven enhet' }),
    );
    await userEvent.type(
      screen.getByRole('textbox', { name: 'Pris per angiven enhet' }),
      '199.95',
    );
    await userEvent.click(
      screen.getByRole('button', { name: 'Spara alternativ' }),
    );
    await waitFor(() => expect(saved).toHaveBeenCalledOnce());
    expect(researchApi.saveOption).toHaveBeenCalledWith(
      detail.requestId,
      expect.objectContaining({
        priceMinorUnits: 19995,
        quantity: 6,
        notes: 'SECRET PRIVATE NOTE',
      }),
      item.id,
      item.versionToken,
    );
  });
  it('shows multi-leg timezone inputs and explicit leg add/remove controls', async () => {
    mount(
      <ResearchOptionEditor
        requestId={detail.requestId}
        kind="Flight"
        onSaved={vi.fn()}
        onClose={vi.fn()}
      />,
    );
    expect(
      screen.getByRole('textbox', { name: 'Avgångens tidszon' }),
    ).toBeInTheDocument();
    await userEvent.click(
      screen.getByRole('button', { name: 'Lägg till flygsträcka' }),
    );
    expect(
      screen.getAllByRole('textbox', { name: 'Avgångens tidszon' }),
    ).toHaveLength(2);
    await userEvent.click(
      screen.getByRole('button', { name: 'Ta bort Flygsträcka 2' }),
    );
    expect(
      screen.getAllByRole('textbox', { name: 'Avgångens tidszon' }),
    ).toHaveLength(1);
  });
  it('compares selected alternatives without currency conversion', async () => {
    const other = {
      ...item,
      id: 'second',
      option: { ...item.option, title: 'TEST EUR hotel', currency: 'EUR' },
    };
    mount(
      <ResearchOptions
        requestId={detail.requestId}
        options={[item, other]}
        eligible
        onSaved={vi.fn()}
      />,
    );
    await userEvent.click(
      screen.getByRole('checkbox', {
        name: 'Välj för jämförelse: ' + item.option.title,
      }),
    );
    await userEvent.click(
      screen.getByRole('checkbox', {
        name: 'Välj för jämförelse: TEST EUR hotel',
      }),
    );
    await userEvent.click(
      screen.getByRole('button', { name: 'Jämför valda alternativ' }),
    );
    expect(
      screen.getByRole('heading', { name: 'Jämförelse' }),
    ).toBeInTheDocument();
    expect(screen.getAllByText('TEST EUR hotel')).toHaveLength(2);
  });
  it('preserves item ordering and uses explicit agent recommendations', async () => {
    const saved = vi.fn();
    mount(
      <ProposalEditor
        detail={detail}
        options={[item]}
        configuration={{
          blockingReasons: [],
          termsSv: proposal.termsSnapshot,
          termsEn: proposal.termsSnapshot,
        }}
        onSaved={saved}
      />,
    );
    const choices = screen.getAllByRole('checkbox', {
      name: 'Markera som rekommenderat',
    });
    await userEvent.click(choices[0]!);
    expect(choices[0]).toBeChecked();
    expect(choices[1]).not.toBeChecked();
    await userEvent.click(
      screen.getByRole('button', { name: 'Spara reseförslag' }),
    );
    await waitFor(() => expect(saved).toHaveBeenCalledOnce());
    expect(proposalApi.save).toHaveBeenCalledWith(
      detail.id,
      expect.objectContaining({
        alternatives: [
          expect.objectContaining({ recommended: true, itemIds: [item.id] }),
          expect.objectContaining({ recommended: false, itemIds: [item.id] }),
        ],
      }),
    );
    expect(reorder(['a', 'b', 'c'], 1, -1)).toEqual(['b', 'a', 'c']);
  });
  it('uses the same presentation for preview and customer snapshots', () => {
    mount(<ProposalSummary proposal={proposal} />);
    expect(
      screen.getByRole('heading', { name: 'TEST trip' }),
    ).toBeInTheDocument();
    expect(screen.getByText('Reseagentens rekommendation')).toBeInTheDocument();
    expect(screen.getAllByText('TEST cancellation terms')).toHaveLength(2);
  });
  it('excludes time between outbound and return journeys from flight duration', () => {
    const base = {
      journey: 1,
      airline: 'TEST',
      flightNumber: '',
      departureAirport: 'ARN',
      arrivalAirport: 'LIS',
      departureTimeZone: 'Europe/Stockholm',
      arrivalTimeZone: 'Europe/Lisbon',
      departureAt: '2027-06-01T10:00:00+02:00',
      arrivalAt: '2027-06-01T13:00:00+01:00',
    };
    const back = {
      ...base,
      journey: 2,
      departureAt: '2027-06-07T10:00:00+01:00',
      arrivalAt: '2027-06-07T14:00:00+02:00',
    };
    expect(flightMetrics([base, back])).toEqual({
      connections: 0,
      durationHours: 7,
    });
  });
  it('renders the saved draft editor and actions without duplicate sibling keys', async () => {
    vi.mocked(proposalApi.detail).mockResolvedValue(detail);
    vi.mocked(proposalApi.configuration).mockResolvedValue({
      blockingReasons: [],
      termsSv: proposal.termsSnapshot,
      termsEn: proposal.termsSnapshot,
    });
    vi.mocked(proposalApi.history).mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 50,
      totalCount: 0,
      totalPages: 0,
    });
    vi.mocked(researchApi.workspace).mockResolvedValue({
      eligible: true,
      status: 'InProgress',
      versionToken: 'TEST',
      notes: '',
      options: [item],
      proposals: {
        items: [],
        page: 1,
        pageSize: 20,
        totalCount: 0,
        totalPages: 0,
      },
    });
    const log = vi.spyOn(console, 'error').mockImplementation(() => {});
    try {
      mount(<AdminProposalPage />);
      await screen.findByRole('button', { name: 'Spara reseförslag' });
      expect(
        log.mock.calls.some((args) =>
          args.some(
            (arg) => typeof arg === 'string' && arg.includes('same key'),
          ),
        ),
      ).toBe(false);
    } finally {
      log.mockRestore();
    }
  });
  it('reloads after a failed research removal without reporting a confirmed save', async () => {
    const saved = vi.fn();
    const reload = vi.fn();
    vi.mocked(researchApi.removeOption).mockRejectedValue(
      new ApiError('conflict', 409),
    );
    mount(
      <ResearchOptions
        requestId={detail.requestId}
        options={[item]}
        eligible
        onSaved={saved}
        onRefresh={reload}
      />,
    );
    await userEvent.click(
      screen.getByRole('button', { name: 'Ta bort: ' + item.option.title }),
    );
    await userEvent.click(screen.getByRole('button', { name: 'Ja, bekräfta' }));
    await screen.findByRole('alert');
    await userEvent.click(
      screen.getByRole('button', { name: 'Hämta aktuell status' }),
    );
    expect(reload).toHaveBeenCalledOnce();
    expect(saved).not.toHaveBeenCalled();
  });
});
