import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { expect, it } from 'vitest';
import { App } from '../../app/App';
import { branding } from '../../app/config/branding';
import { routes } from '../../app/config/routes';
import { sv } from '../../content/sv';

function renderHome() {
  return render(
    <MemoryRouter>
      <App />
    </MemoryRouter>,
  );
}

it('renders the complete Swedish homepage in the required section order', () => {
  renderHome();
  expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
    sv.hero.title.join(' '),
  );
  const headings = screen
    .getAllByRole('heading', { level: 2 })
    .map((heading) => heading.textContent);
  expect(headings.slice(0, 6)).toEqual([
    sv.services.title,
    sv.process.title,
    sv.benefits.title.join(' '),
    sv.about.title,
    sv.inspiration.title,
    sv.finalCta.title,
  ]);
  expect(screen.queryByText(sv.diagnostics.connected)).not.toBeInTheDocument();
});

it('renders exactly four service cards with useful links and no invented prices', () => {
  renderHome();
  const services = screen.getByRole('region', { name: sv.services.title });
  expect(within(services).getAllByRole('article')).toHaveLength(4);
  for (const service of sv.services.items) {
    expect(
      within(services).getByRole('heading', { name: service.title }),
    ).toBeInTheDocument();
    expect(
      within(services).getByRole('link', {
        name: service.action,
      }),
    ).toHaveAttribute('href', routes.planTrip);
  }
  expect(services).not.toHaveTextContent(/\d+\s*(SEK|kr)/);
});

it('sends the primary CTA to an honest non-submitting future request page', async () => {
  const user = userEvent.setup();
  renderHome();
  const hero = screen.getByRole('region', { name: sv.hero.title.join(' ') });
  await user.click(within(hero).getByRole('link', { name: sv.actions.plan }));
  expect(
    screen.getByRole('heading', { level: 1, name: sv.comingSoon.title }),
  ).toBeInTheDocument();
  expect(screen.getByText(sv.comingSoon.note)).toBeInTheDocument();
  expect(screen.queryByRole('textbox')).not.toBeInTheDocument();
  await user.click(screen.getByRole('link', { name: sv.actions.home }));
  expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
    sv.hero.title.join(' '),
  );
});

it('provides a clearly labeled about preview without inventing an identity', async () => {
  const user = userEvent.setup();
  renderHome();
  expect(screen.getByText(sv.about.portrait)).toBeInTheDocument();
  await user.click(screen.getByRole('link', { name: sv.actions.about }));
  expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
    sv.comingSoon.aboutTitle,
  );
});

it('uses meaningful image alternatives and non-link legal/contact placeholders', () => {
  renderHome();
  expect(screen.getAllByRole('img')).toHaveLength(4);
  for (const image of screen.getAllByRole('img'))
    expect(image).toHaveAttribute('alt', expect.stringMatching(/Illustrativ/));
  const footer = screen.getByRole('contentinfo');
  expect(
    within(footer).getByRole('link', { name: branding.name }),
  ).toHaveAttribute('href', routes.home);
  expect(
    within(footer).getByText(sv.footer.contactPlaceholder),
  ).toBeInTheDocument();
  expect(within(footer).getByText(sv.footer.privacy).tagName).toBe('SPAN');
  expect(within(footer).getByText(sv.footer.terms).tagName).toBe('SPAN');
});

it('sets localized homepage metadata without exposing a launch-ready indexing claim', () => {
  renderHome();
  expect(document.title).toBe(`${sv.meta.homeTitle} | ${branding.name}`);
  expect(document.querySelector('meta[name="description"]')).toHaveAttribute(
    'content',
    sv.meta.description,
  );
  expect(document.querySelector('meta[name="robots"]')).toHaveAttribute(
    'content',
    'noindex, nofollow',
  );
});
