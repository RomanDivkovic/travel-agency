import { usePageMetadata } from '../../hooks/usePageMetadata';
import { PricingPage } from '../public/PricingPage';
import { TextField } from './Fields';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { beforeEach, expect, it, vi } from 'vitest';
import { LanguageProvider } from '../../localization/LanguageProvider';
import {
  languageStorageKey,
  useTranslations,
} from '../../localization/useLanguage';
import { WebsiteContext, applyFields } from './contentContext';
import { cmsApi } from './api';
import { ContentEditorPage } from './ManagementPages';
import { ApiError } from '../../services/apiClient';
import type { ContentDetail, PreferencesData, PublicContent } from './types';
import { FaqPage } from '../public/FaqPage';
import { ServiceFee } from './PublishedComponents';
vi.mock('./api', () => ({
  cmsApi: {
    detail: vi.fn(),
    media: vi.fn(),
    save: vi.fn(),
    publish: vi.fn(),
    restore: vi.fn(),
    list: vi.fn(),
  },
}));
const preferences: PreferencesData = {
  agentMessageEmail: true,
  customerMessageEmail: true,
};
const detail: ContentDetail = {
  key: 'preferences',
  versionToken: '00000000-0000-0000-0000-000000000000',
  draft: preferences,
  publishedVersion: 0,
  updatedAt: null,
  updatedBy: null,
  versions: [],
};
function show(ui: React.ReactNode, content: PublicContent = { documents: {} }) {
  return render(
    <MemoryRouter>
      <LanguageProvider>
        <WebsiteContext value={content}>{ui}</WebsiteContext>
      </LanguageProvider>
    </MemoryRouter>,
  );
}
beforeEach(() => {
  localStorage.setItem(languageStorageKey, 'en');
  vi.mocked(cmsApi.detail).mockResolvedValue(structuredClone(detail));
  vi.mocked(cmsApi.media).mockResolvedValue([]);
  vi.mocked(cmsApi.save).mockResolvedValue();
  vi.mocked(cmsApi.publish).mockResolvedValue();
});
it('does not publish on save and requires an explicit confirmation after backend persistence', async () => {
  show(<ContentEditorPage documentKey="preferences" />);
  const user = userEvent.setup();
  await screen.findByRole('button', { name: 'Save draft' });
  expect(screen.getByRole('button', { name: 'Publish' })).toBeDisabled();
  const saved = { ...detail, versionToken: 'saved-token' };
  vi.mocked(cmsApi.detail).mockResolvedValue(saved);
  await user.click(screen.getByRole('button', { name: 'Save draft' }));
  await screen.findByText('Draft saved. It has not been published.');
  expect(cmsApi.publish).not.toHaveBeenCalled();
  expect(cmsApi.save).toHaveBeenCalledWith(
    'preferences',
    detail.versionToken,
    preferences,
  );
  await user.click(
    screen.getByLabelText(
      'I have reviewed Swedish and English and want to publish this.',
    ),
  );
  await user.click(screen.getByRole('button', { name: 'Publish' }));
  await waitFor(() =>
    expect(cmsApi.publish).toHaveBeenCalledWith('preferences', 'saved-token'),
  );
});
it('retains edits and prevents publication after a stale save', async () => {
  vi.mocked(cmsApi.detail).mockResolvedValue({
    ...detail,
    versionToken: 'current',
  });
  vi.mocked(cmsApi.save).mockRejectedValue(new ApiError('Conflict', 409));
  show(<ContentEditorPage documentKey="preferences" />);
  const user = userEvent.setup();
  const check = await screen.findByLabelText(
    'Notify the agent of customer messages',
  );
  await user.click(check);
  await user.click(screen.getByRole('button', { name: 'Save draft' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Content changed');
  expect(check).not.toBeChecked();
  expect(screen.getByRole('button', { name: 'Publish' })).toBeDisabled();
});
it('does not claim a draft was saved until the server confirms', async () => {
  let finish!: () => void;
  vi.mocked(cmsApi.save).mockReturnValue(
    new Promise<void>((r) => {
      finish = r;
    }),
  );
  show(<ContentEditorPage documentKey="preferences" />);
  await userEvent.click(
    await screen.findByRole('button', { name: 'Save draft' }),
  );
  expect(
    screen.queryByText('Draft saved. It has not been published.'),
  ).not.toBeInTheDocument();
  finish();
  await screen.findByText('Draft saved. It has not been published.');
});
it('renders published FAQ text safely and excludes disabled questions', () => {
  show(<FaqPage />, {
    documents: {
      'faq:one': {
        enabled: true,
        displayOrder: 1,
        sv: { question: 'SV', answer: 'SV' },
        en: {
          question: 'TEST <script>text</script>',
          answer: 'literal <img src=x onerror=alert(1)>',
        },
      },
      'faq:two': {
        enabled: false,
        displayOrder: 0,
        sv: { question: 'hidden', answer: 'hidden' },
        en: { question: 'hidden', answer: 'hidden' },
      },
    },
  });
  expect(screen.getByText('TEST <script>text</script>')).toBeVisible();
  expect(screen.queryByText('hidden')).not.toBeInTheDocument();
  expect(document.querySelector('script')).toBeNull();
  expect(document.querySelector('img')).toBeNull();
});
it('only overlays existing textual fields and preserves routing identifiers', () => {
  const source = {
    hero: { title: 'original' },
    navigation: { destination: 'home' },
  };
  const result = applyFields(
    source,
    {
      'page:home': {
        sv: {
          'shared.hero.title': 'changed',
          'shared.navigation.destination': 'javascript:evil',
          'shared.__proto__.bad': 'bad',
          'shared.unknown': 'no',
        },
        en: {},
        image: null,
      },
    },
    'shared',
    'sv',
  );
  expect(source.hero.title).toBe('original');
  expect(result.hero.title).toBe('changed');
  expect(result.navigation.destination).toBe('home');
  expect(Object.hasOwn(result, 'unknown')).toBe(false);
});
it('keeps the default website copy when no CMS content exists', () => {
  function Title() {
    return <h1>{useTranslations().hero.title.join(' ')}</h1>;
  }
  show(<Title />);
  expect(screen.getByRole('heading')).toHaveTextContent('Your next journey');
});
it('shows the quotation fallback when configured pricing has expired', () => {
  show(
    <ServiceFee
      service={{
        code: 'FlightsOnly',
        enabled: true,
        displayOrder: 0,
        sv: {
          name: '',
          shortDescription: '',
          fullDescription: '',
          included: '',
          excluded: '',
          suitable: '',
          cta: '',
        },
        en: {
          name: '',
          shortDescription: '',
          fullDescription: '',
          included: '',
          excluded: '',
          suitable: '',
          cta: '',
        },
        pricing: {
          type: 'FixedFee',
          amountMinor: 70000,
          currency: 'SEK',
          effectiveFrom: '2020-01-01T00:00:00Z',
          effectiveUntil: '2021-01-01T00:00:00Z',
        },
      }}
    />,
  );
  expect(screen.getByText('Personal quotation')).toBeVisible();
  expect(screen.queryByText(/700/)).not.toBeInTheDocument();
});

it('labels a populated textarea independently of its current content', async () => {
  show(
    <TextField
      label="Short description"
      type="textarea"
      value="Existing copy"
      onChange={() => {}}
    />,
  );
  expect(
    screen.getByLabelText('Short description', { exact: true }),
  ).toHaveValue('Existing copy');
});

it('uses the published business name for page metadata', () => {
  function Meta() {
    usePageMetadata('TEST title');
    return <h1>TEST title</h1>;
  }
  const business = {
    businessName: 'TEST agency',
    legalBusinessName: '',
    organizationNumber: '',
    contactEmail: '',
    contactPhone: '',
    websiteUrl: '',
    address: '',
    postalCode: '',
    city: '',
    country: 'SE',
    defaultLanguage: 'sv' as const,
    defaultCurrency: 'SEK',
    timeZone: 'Europe/Stockholm',
    publicContactVisible: true,
    openingHours: [],
    exceptions: [],
  };
  show(<Meta />, { documents: { business } });
  expect(document.title).toBe('TEST title | TEST agency');
  expect(
    document.querySelector('meta[property="og:site_name"]'),
  ).toHaveAttribute('content', 'TEST agency');
});
it('shows editable quotation copy alongside configured fees', () => {
  show(<PricingPage />, {
    documents: {
      'page:pricing': {
        sv: {},
        en: {
          'public.pricing.quoteText': 'TEST published quotation explanation',
        },
        image: null,
      },
    },
  });
  expect(
    screen.getByText('TEST published quotation explanation'),
  ).toBeVisible();
});
