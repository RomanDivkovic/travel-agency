import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { beforeEach, expect, it, vi } from 'vitest';
import { LanguageProvider } from '../../localization/LanguageProvider';
import { languageStorageKey } from '../../localization/useLanguage';
import { ApiError } from '../../services/apiClient';
import { documentApi, saveBlob } from './api';
import { DocumentUpload } from './DocumentUpload';
import { DocumentCard } from './DocumentCard';
import { ItineraryTimeline } from './ItineraryTimeline';
import { ItineraryEditor } from './ItineraryEditor';
import { travelTime, localInput } from './time';
import type {
  DocumentLibrary,
  TravelDocument,
  Itinerary,
  ItineraryVersion,
} from './types';
vi.mock('./api', () => ({
  documentApi: {
    upload: vi.fn(),
    change: vi.fn(),
    edit: vi.fn(),
    file: vi.fn(),
    save: vi.fn(),
  },
  saveBlob: vi.fn(),
}));
const document: TravelDocument = {
  id: 'doc',
  bookingId: 'booking',
  reservationId: null,
  reservationTitle: null,
  type: 'FlightTicket',
  currentVersion: 1,
  publishedVersion: null,
  versionToken: 'token',
  versions: [
    {
      id: 'version',
      version: 1,
      name: 'TEST <script> ticket',
      description: 'TEST public description',
      originalFileName: 'ticket.pdf',
      contentType: 'application/pdf',
      size: 400,
      status: 'Draft',
      scan: 'Quarantined',
      createdAt: '2026-11-05T12:00:00Z',
      publishedAt: null,
    },
  ],
};
const library: DocumentLibrary = {
  reference: 'BKG-TEST',
  items: [document],
  page: 1,
  totalPages: 1,
  totalCount: 1,
  maxBytes: 1024,
  reservations: [],
};
const version: ItineraryVersion = {
  id: 'v',
  version: 1,
  status: 'Draft',
  title: 'TEST Lisbon trip',
  description: 'TEST overview',
  createdAt: '2026-11-05T12:00:00Z',
  publishedAt: null,
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
    children: 1,
    rooms: 1,
  },
  entries: [
    {
      id: 'entry',
      kind: 'Flight',
      event: 'Departure',
      title: 'TEST flight',
      description: 'TEST baggage',
      date: '2027-06-01',
      at: '2027-06-01T10:00:00+02:00',
      timeZone: 'Europe/Stockholm',
      location: 'ARN',
      supplierReference: 'TEST-REF',
      confirmed: true,
      sortOrder: 0,
    },
  ],
};
const itinerary: Itinerary = {
  bookingId: 'booking',
  reference: 'BKG-TEST',
  versionToken: 'token',
  currentVersion: 1,
  publishedVersion: null,
  sourceChanged: false,
  versions: [version],
};
function mount(node: React.ReactNode) {
  return render(
    <MemoryRouter>
      <LanguageProvider>{node}</LanguageProvider>
    </MemoryRouter>,
  );
}
beforeEach(() => {
  vi.resetAllMocks();
  localStorage.setItem(languageStorageKey, 'en');
});
it('uploads a private draft only after explicit non-identity confirmation and server success', async () => {
  let finish!: (v: { id: string }) => void;
  vi.mocked(documentApi.upload).mockReturnValue(
    new Promise((r) => {
      finish = r;
    }),
  );
  const saved = vi.fn();
  mount(
    <DocumentUpload bookingId="booking" library={library} onSaved={saved} />,
  );
  const u = userEvent.setup();
  await u.type(screen.getByLabelText('Display name'), 'TEST ticket');
  await u.upload(
    screen.getByLabelText('File (PDF, JPEG or PNG)'),
    new File(['%PDF-TEST'], 'test.pdf', { type: 'application/pdf' }),
  );
  await u.click(screen.getByRole('checkbox'));
  fireEvent.submit(
    screen.getByRole('button', { name: 'Upload document' }).closest('form')!,
  );
  expect(saved).not.toHaveBeenCalled();
  expect(screen.getByRole('button', { name: 'Saving…' })).toBeDisabled();
  await act(async () => finish({ id: 'new' }));
  await waitFor(() => expect(saved).toHaveBeenCalledOnce());
  expect(documentApi.upload).toHaveBeenCalledWith(
    'booking',
    expect.any(File),
    expect.objectContaining({ noIdentityDocuments: true, name: 'TEST ticket' }),
    undefined,
  );
  expect(documentApi.change).not.toHaveBeenCalled();
});
it('shows an upload rejection without claiming the file was saved', async () => {
  vi.mocked(documentApi.upload).mockRejectedValue(
    new ApiError('signature', 415),
  );
  const saved = vi.fn();
  mount(
    <DocumentUpload bookingId="booking" library={library} onSaved={saved} />,
  );
  const u = userEvent.setup();
  await u.type(screen.getByLabelText('Display name'), 'TEST');
  await u.upload(
    screen.getByLabelText('File (PDF, JPEG or PNG)'),
    new File(['not a PDF'], 'test.pdf', { type: 'application/pdf' }),
  );
  await u.click(screen.getByRole('checkbox'));
  fireEvent.submit(
    screen.getByRole('button', { name: 'Upload document' }).closest('form')!,
  );
  expect(await screen.findByRole('alert')).toHaveTextContent(
    'signature must agree',
  );
  expect(saved).not.toHaveBeenCalled();
});
it('does not offer quarantined publication, preview or download', () => {
  mount(
    <DocumentCard
      document={document}
      library={library}
      admin
      onSaved={vi.fn()}
    />,
  );
  expect(screen.getByText(/Quarantined: scanning/)).toBeVisible();
  expect(
    screen.queryByRole('button', { name: 'Publish' }),
  ).not.toBeInTheDocument();
  expect(
    screen.queryByRole('button', { name: 'Preview' }),
  ).not.toBeInTheDocument();
  expect(
    screen.queryByRole('button', { name: 'Download' }),
  ).not.toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Scan again' })).toBeVisible();
  expect(globalThis.document.querySelector('script')).toBeNull();
});
it('downloads only through the owned document API and omits administrator controls', async () => {
  const d: TravelDocument = {
    ...document,
    publishedVersion: 1,
    versionToken: undefined,
    versions: [
      { ...document.versions[0]!, status: 'Published', scan: 'Clean' },
    ],
  };
  const blob = new Blob(['%PDF-TEST']);
  vi.mocked(documentApi.file).mockResolvedValue(blob);
  mount(
    <DocumentCard
      document={d}
      library={library}
      admin={false}
      onSaved={vi.fn()}
    />,
  );
  await userEvent.click(screen.getByRole('button', { name: 'Download' }));
  await waitFor(() =>
    expect(saveBlob).toHaveBeenCalledWith(blob, 'ticket.pdf'),
  );
  expect(documentApi.file).toHaveBeenCalledWith('booking', 'doc', false, 1);
  expect(screen.queryByText('Version history')).not.toBeInTheDocument();
  expect(
    screen.queryByRole('button', { name: 'Replace document' }),
  ).not.toBeInTheDocument();
});
it('renders supplier-local time and clearly identifies informational entries', () => {
  mount(
    <ItineraryTimeline
      version={{
        ...version,
        entries: [
          ...version.entries,
          {
            ...version.entries[0]!,
            id: 'info',
            kind: 'Custom',
            event: 'Information',
            title: 'Airport guidance',
            confirmed: false,
            supplierReference: '',
          },
        ],
      }}
    />,
  );
  expect(screen.getAllByText(/10:00/)).toHaveLength(2);
  expect(screen.getByText('Supplier-confirmed information')).toBeVisible();
  expect(screen.getByText(/Information \/ recommendation/)).toBeVisible();
  expect(screen.getByText(/1 June 2027/)).toBeVisible();
});
it('distinguishes repeated daylight-saving times by explicit offsets', () => {
  expect(
    travelTime('2027-10-31T02:30:00+02:00', 'Europe/Stockholm', 'en'),
  ).toContain('GMT+2');
  expect(
    travelTime('2027-10-31T02:30:00+01:00', 'Europe/Stockholm', 'en'),
  ).toContain('GMT+1');
  expect(
    travelTime('2027-06-01T10:00:00+02:00', 'Europe/Lisbon', 'en'),
  ).toContain('09:00');
  expect(localInput('2027-06-01T08:00:00Z', 'Europe/Stockholm').time).toBe(
    '2027-06-01T10:00',
  );
  expect(travelTime('bad', 'bad', 'en')).toBeNull();
});
it('edits custom entries without sending authoritative supplier fields back', async () => {
  vi.mocked(documentApi.save).mockResolvedValue(undefined);
  const saved = vi.fn();
  mount(<ItineraryEditor data={itinerary} version={version} onSaved={saved} />);
  const u = userEvent.setup();
  expect(screen.queryByDisplayValue('TEST flight')).not.toBeInTheDocument();
  await u.click(
    screen.getByRole('button', { name: 'Add informational entry' }),
  );
  await u.type(screen.getByLabelText('Entry title'), 'Airport guidance');
  fireEvent.change(screen.getByLabelText('Local travel date'), {
    target: { value: '2027-06-01' },
  });
  await u.click(screen.getByRole('button', { name: 'Save draft' }));
  await waitFor(() => expect(saved).toHaveBeenCalledOnce());
  expect(documentApi.save).toHaveBeenCalledWith(
    'booking',
    expect.objectContaining({
      versionToken: 'token',
      customEntries: [
        expect.objectContaining({
          title: 'Airport guidance',
          date: '2027-06-01',
          localTime: null,
        }),
      ],
    }),
  );
  expect(vi.mocked(documentApi.save).mock.calls[0]![1]).not.toHaveProperty(
    'entries',
  );
});
it('renders Swedish timeline labels', () => {
  localStorage.setItem(languageStorageKey, 'sv');
  mount(<ItineraryTimeline version={version} />);
  expect(screen.getByText('Leverantörsbekräftad uppgift')).toBeVisible();
  expect(screen.getByText(/Avgång · TEST flight/)).toBeVisible();
});
