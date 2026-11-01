import { fireEvent, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { beforeEach, expect, it, vi } from 'vitest';
import { App } from '../../app/App';
import { languageStorageKey } from '../../localization/useLanguage';
import { publicCopy } from '../../content/public';
import { validContactEmail } from '../../app/config/contact';
import { buildContactDraft } from './contactDraft';

vi.mock('../../app/config/contact', async (importOriginal) => {
  const actual =
    await importOriginal<typeof import('../../app/config/contact')>();
  return {
    ...actual,
    get contactEmail() {
      return mailbox.value;
    },
  };
});
const mailbox = vi.hoisted(() => ({ value: undefined as string | undefined }));
beforeEach(() => {
  mailbox.value = undefined;
});
function show(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  );
}

for (const language of ['sv', 'en'] as const) {
  for (const page of [
    'services',
    'pricing',
    'about',
    'contact',
    'faq',
  ] as const) {
    it(`renders ${page} with localized content and metadata in ${language}`, () => {
      localStorage.setItem(languageStorageKey, language);
      show(`/${page}`);
      const copy = publicCopy[language][page];
      expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
        copy.title,
      );
      expect(document.title).toContain(copy.title);
      expect(document.documentElement.lang).toBe(language);
      expect(
        document.querySelector('meta[name="description"]'),
      ).toHaveAttribute('content', copy.description);
      expect(
        document.querySelector('meta[property="og:locale"]'),
      ).toHaveAttribute('content', language === 'sv' ? 'sv_SE' : 'en_GB');
      expect(
        screen
          .getByRole('navigation', {
            name: language === 'sv' ? 'Huvudnavigation' : 'Main navigation',
          })
          .querySelector('[aria-current="page"]'),
      ).toHaveAttribute('href', `/${page}`);
    });
  }
}
it('switches on the current route, persists across navigation and remount, and returns to Swedish', async () => {
  const user = userEvent.setup();
  const app = show('/services');
  await user.selectOptions(screen.getByLabelText('Språk'), 'en');
  expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
    publicCopy.en.services.title,
  );
  expect(localStorage.getItem(languageStorageKey)).toBe('en');
  await user.click(
    within(
      screen.getByRole('navigation', { name: 'Main navigation' }),
    ).getByRole('link', { name: 'Pricing' }),
  );
  expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
    publicCopy.en.pricing.title,
  );
  app.unmount();
  show('/');
  expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
    'Your next journey starts here.',
  );
  await user.selectOptions(screen.getByLabelText('Language'), 'sv');
  expect(document.documentElement.lang).toBe('sv');
  expect(localStorage.getItem(languageStorageKey)).toBe('sv');
});
it('defaults safely for an invalid saved language and blocked storage', async () => {
  localStorage.setItem(languageStorageKey, 'fr');
  const app = show('/faq');
  expect(document.documentElement.lang).toBe('sv');
  app.unmount();
  const read = vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
    throw new Error('blocked');
  });
  const write = vi
    .spyOn(Storage.prototype, 'setItem')
    .mockImplementation(() => {
      throw new Error('blocked');
    });
  try {
    show('/faq');
    await userEvent.setup().selectOptions(screen.getByLabelText('Språk'), 'en');
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
      publicCopy.en.faq.title,
    );
  } finally {
    read.mockRestore();
    write.mockRestore();
  }
});
it('uses native, independently operable FAQ disclosures with complete answers', () => {
  show('/faq');
  const details = document.querySelectorAll('details');
  expect(details).toHaveLength(publicCopy.sv.faq.items.length);
  const first = details[0]!;
  fireEvent.click(first.querySelector('summary')!);
  expect(first.open).toBe(true);
  expect(first).toHaveTextContent(publicCopy.sv.faq.items[0].answer);
  fireEvent.click(first.querySelector('summary')!);
  expect(first.open).toBe(false);
});
it('disables contact submission when a real mailbox is absent', () => {
  show('/contact');
  expect(
    screen.getByText(publicCopy.sv.contact.unavailable),
  ).toBeInTheDocument();
  expect(
    screen.getByRole('button', { name: publicCopy.sv.contact.prepare }),
  ).toBeDisabled();
  expect(screen.getByLabelText(publicCopy.sv.contact.message)).toBeDisabled();
  expect(screen.queryByRole('status')).not.toBeInTheDocument();
});
it('validates contact fields and prepares an encoded draft without claiming delivery', async () => {
  mailbox.value = 'owner@example.com';
  const user = userEvent.setup();
  show('/contact');
  const form = screen
    .getByRole('button', { name: publicCopy.sv.contact.prepare })
    .closest('form')!;
  fireEvent.submit(form);
  expect(screen.getByRole('alert')).toHaveTextContent(
    publicCopy.sv.contact.error,
  );
  await user.type(screen.getByLabelText('Namn'), 'Anna');
  await user.type(
    screen.getByLabelText('Din e-postadress'),
    'anna@example.com',
  );
  await user.type(screen.getByLabelText('Ämne'), 'Hej & hello?');
  await user.type(screen.getByLabelText('Meddelande'), 'En allmän fråga.');
  await user.click(
    screen.getByRole('button', { name: publicCopy.sv.contact.prepare }),
  );
  expect(screen.getByRole('status')).toHaveTextContent(
    'Inget meddelande har skickats',
  );
  expect(
    screen.getByRole('link', { name: publicCopy.sv.contact.submit }),
  ).toHaveAttribute(
    'href',
    buildContactDraft(mailbox.value, {
      name: 'Anna',
      email: 'anna@example.com',
      subject: 'Hej & hello?',
      message: 'En allmän fråga.',
    }),
  );
  await user.type(screen.getByLabelText('Meddelande'), ' Ny text');
  expect(screen.queryByRole('status')).not.toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('Namn'), { target: { value: '   ' } });
  fireEvent.submit(form);
  expect(screen.getByRole('alert')).toBeInTheDocument();
});
it('rejects invalid mailbox configuration and encodes header-like input as content', () => {
  for (const value of [
    '',
    'not-an-address',
    'mail@example.com\r\nBcc:other@example.com',
    undefined,
  ])
    expect(validContactEmail(value)).toBeUndefined();
  expect(validContactEmail(' owner@example.com ')).toBe('owner@example.com');
  const draft = buildContactDraft('owner@example.com', {
    name: 'A',
    email: 'a@example.com',
    subject: 'Trip & test\nBcc: x',
    message: 'A? & B#C',
  });
  expect(draft).toContain('subject=Trip%20%26%20test%0ABcc%3A%20x');
  expect(draft.match(/&/g)).toHaveLength(1);
});
it('keeps the legacy about route working and opens the request wizard', () => {
  const app = show('/about-preview');
  expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
    publicCopy.sv.about.title,
  );
  app.unmount();
  show('/plan-your-trip');
  expect(screen.getAllByRole('radio')).toHaveLength(4);
});
it('emits canonical links only for configured HTTPS origins', () => {
  vi.stubEnv('VITE_SITE_URL', 'https://travel.example/base');
  const app = show('/pricing');
  expect(document.querySelector('link[rel="canonical"]')).toHaveAttribute(
    'href',
    'https://travel.example/pricing',
  );
  app.unmount();
  vi.stubEnv('VITE_SITE_URL', 'http://localhost:5173');
  show('/faq');
  expect(
    document.querySelector('link[rel="canonical"]'),
  ).not.toBeInTheDocument();
  vi.unstubAllEnvs();
});
