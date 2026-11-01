import {
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useLocation } from 'react-router';
import { expect, it, vi } from 'vitest';
import { App } from '../../app/App';
import { languageStorageKey } from '../../localization/useLanguage';
import { draftKey, storeDraft } from './draft';
import { services, type Service } from './model';
import { validWizard } from './testData';
function LocationProbe() {
  const location = useLocation();
  return (
    <span hidden data-testid="route-location">
      {location.pathname}
      {location.search}
    </span>
  );
}
function show(path = '/plan-your-trip') {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
      <LocationProbe />
    </MemoryRouter>,
  );
}
function change(id: string, value: string) {
  fireEvent.change(document.getElementById(id)!, { target: { value } });
}
async function next() {
  await userEvent
    .setup()
    .click(screen.getByRole('button', { name: 'Continue' }));
}
async function toReview(service: Service) {
  localStorage.setItem(languageStorageKey, 'en');
  show(`/plan-your-trip?service=${service}`);
  await next();
  if (service !== 'accommodation') change('origin', 'Stockholm');
  change('place', 'Lisbon');
  await next();
  const data = validWizard(service);
  change('departure', data.dates.departure);
  change('return', data.dates.return);
  await next();
  change('adults', '2');
  change('children', '1');
  change('age-0', '7');
  if (service !== 'flights') change('rooms', '2');
  await next();
  change('amount', '25000');
  await next();
  if (service !== 'accommodation') {
    change('priority', 'direct');
    await next();
  }
  if (service !== 'flights') {
    change('accommodationType', 'hotel');
    await userEvent.setup().click(screen.getByLabelText('Breakfast preferred'));
    await next();
  }
  if (service === 'complete') {
    change('pace', 'active');
    await userEvent.setup().click(screen.getByLabelText('Nature'));
    await next();
  }
  change('special', 'Near a station');
  await next();
  change('name', 'Anna');
  change('email', 'anna@example.com');
  await next();
}
for (const service of services)
  it(`completes the ${service} journey, reviews answers and enables explicit submission`, async () => {
    await toReview(service);
    expect(
      screen.getByRole('heading', { level: 2, name: 'Review your journey' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent(
      'Review your answers before sending',
    );
    expect(
      screen.getByRole('button', { name: 'Send free enquiry' }),
    ).toBeEnabled();
    expect(screen.getByText('anna@example.com')).toBeInTheDocument();
    expect(screen.getByText('Near a station')).toBeInTheDocument();
    const review =
      document.querySelector('main dl')?.parentElement?.parentElement;
    expect(review).toBeTruthy();
    if (service === 'flights')
      expect(
        screen.queryByRole('heading', {
          level: 3,
          name: 'Accommodation preferences',
        }),
      ).not.toBeInTheDocument();
    if (service === 'accommodation')
      expect(
        screen.queryByRole('heading', { level: 3, name: 'Flight preferences' }),
      ).not.toBeInTheDocument();
  });
it('allows review edits, preserves all other answers and revalidates the whole request', async () => {
  await toReview('complete');
  const user = userEvent.setup();
  await user.click(screen.getByRole('button', { name: 'Edit: Budget' }));
  change('amount', '0');
  await user.click(screen.getByRole('button', { name: 'Back to review' }));
  expect(screen.getByRole('alert')).toBeInTheDocument();
  expect(
    screen.getByRole('heading', { level: 2, name: 'Budget' }),
  ).toBeInTheDocument();
  change('amount', '30000');
  await user.click(screen.getByRole('button', { name: 'Back to review' }));
  expect(
    screen.getByRole('heading', { level: 2, name: 'Review your journey' }),
  ).toBeInTheDocument();
  expect(screen.getByText('anna@example.com')).toBeInTheDocument();
  expect(screen.getByText(/30,000/)).toBeInTheDocument();
  await user.click(
    screen.getByRole('button', { name: 'Edit: Choose a service' }),
  );
  await user.click(screen.getByRole('radio', { name: 'Accommodation' }));
  await user.click(screen.getByRole('button', { name: 'Back to review' }));
  expect(
    screen.queryByRole('heading', { level: 3, name: 'Flight preferences' }),
  ).not.toBeInTheDocument();
  expect(
    screen.getByRole('heading', {
      level: 3,
      name: 'Accommodation preferences',
    }),
  ).toBeInTheDocument();
});
it('prevents invalid progression, associates errors with fields and preserves answers on Back', async () => {
  localStorage.setItem(languageStorageKey, 'en');
  show();
  const user = userEvent.setup();
  await next();
  expect(screen.getByRole('alert')).toHaveTextContent('Complete this field');
  await user.click(within(screen.getByRole('alert')).getByRole('link'));
  expect(screen.getByRole('radio', { name: 'Flights' })).toHaveFocus();
  await user.click(screen.getByRole('radio', { name: 'Flights' }));
  await next();
  await next();
  expect(document.getElementById('origin')).toHaveAttribute(
    'aria-invalid',
    'true',
  );
  change('origin', 'Stockholm');
  change('place', 'Oslo');
  await next();
  await user.click(screen.getByRole('button', { name: 'Back' }));
  expect(document.getElementById('origin')).toHaveValue('Stockholm');
  expect(document.getElementById('place')).toHaveValue('Oslo');
});
it('switches languages in place while preserving field data and localized errors', async () => {
  show('/plan-your-trip?service=flights');
  const user = userEvent.setup();
  await user.click(screen.getByRole('button', { name: 'Fortsätt' }));
  change('origin', 'Stockholm');
  await user.selectOptions(screen.getByLabelText('Språk'), 'en');
  expect(document.getElementById('origin')).toHaveValue('Stockholm');
  await next();
  expect(screen.getByRole('alert')).toHaveTextContent('Complete this field');
  await user.selectOptions(screen.getByLabelText('Language'), 'sv');
  expect(screen.getByRole('alert')).toHaveTextContent('Fyll i detta fält');
  expect(document.documentElement.lang).toBe('sv');
});
it('restores only permitted draft data and clears the draft and opt-in on Start over', async () => {
  storeDraft(validWizard('complete'), true);
  const app = show();
  const user = userEvent.setup();
  expect(screen.getByText(/Ett sparat utkast/)).toBeInTheDocument();
  expect(
    screen.getByRole('checkbox', {
      name: 'Spara tillåtna svar på den här enheten',
    }),
  ).toBeChecked();
  await user.click(screen.getByRole('button', { name: 'Fortsätt' }));
  expect(document.getElementById('origin')).toHaveValue('');
  expect(document.getElementById('place')).toHaveValue('');
  await user.click(screen.getByRole('button', { name: 'Börja om' }));
  await user.click(screen.getByRole('button', { name: 'Ja, rensa allt' }));
  expect(localStorage.getItem(draftKey)).toBeNull();
  expect(
    screen.getByRole('checkbox', {
      name: 'Spara tillåtna svar på den här enheten',
    }),
  ).not.toBeChecked();
  expect(
    screen
      .getAllByRole('radio')
      .every((input) => !(input as HTMLInputElement).checked),
  ).toBe(true);
  app.unmount();
  show();
  expect(screen.queryByText(/Ett sparat utkast/)).not.toBeInTheDocument();
});
it('saves only after opting in and survives storage failure', async () => {
  show('/plan-your-trip?service=flights');
  const user = userEvent.setup();
  expect(localStorage.getItem(draftKey)).toBeNull();
  await user.click(
    screen.getByRole('checkbox', {
      name: 'Spara tillåtna svar på den här enheten',
    }),
  );
  expect(localStorage.getItem(draftKey)).toContain('flights');
  await user.click(
    screen.getByRole('checkbox', {
      name: 'Spara tillåtna svar på den här enheten',
    }),
  );
  expect(localStorage.getItem(draftKey)).toBeNull();
  const write = vi
    .spyOn(Storage.prototype, 'setItem')
    .mockImplementation(() => {
      throw Error('quota');
    });
  try {
    await user.click(
      screen.getByRole('checkbox', {
        name: 'Spara tillåtna svar på den här enheten',
      }),
    );
    await waitFor(() =>
      expect(screen.getByRole('status')).toHaveTextContent(
        'Lokal lagring är inte tillgänglig',
      ),
    );
    await user.click(screen.getByRole('button', { name: 'Fortsätt' }));
    expect(
      screen.getByRole('heading', { level: 2, name: 'Resmål' }),
    ).toBeInTheDocument();
  } finally {
    write.mockRestore();
  }
});
it('ignores invalid service query values and unknown or corrupt saved schemas', () => {
  localStorage.setItem(draftKey, '{"version":999,"data":null}');
  show('/plan-your-trip?service=bad');
  expect(
    screen
      .getAllByRole('radio')
      .every((input) => !(input as HTMLInputElement).checked),
  ).toBe(true);
  expect(localStorage.getItem(draftKey)).toBeNull();
});

it('keeps a changed service in the URL and restores it after refresh without reverting to the entry link', async () => {
  localStorage.setItem(languageStorageKey, 'en');
  const app = show('/plan-your-trip?service=flights');
  const user = userEvent.setup();
  await user.click(screen.getByRole('radio', { name: 'Accommodation' }));
  expect(screen.getByTestId('route-location')).toHaveTextContent(
    '/plan-your-trip?service=accommodation',
  );
  await user.click(
    screen.getByRole('checkbox', {
      name: 'Save permitted answers on this device',
    }),
  );
  app.unmount();
  show('/plan-your-trip?service=accommodation');
  expect(screen.getByRole('radio', { name: 'Accommodation' })).toBeChecked();
  await user.click(screen.getByRole('button', { name: 'Start over' }));
  await user.click(
    screen.getByRole('button', { name: 'Yes, clear everything' }),
  );
  expect(screen.getByTestId('route-location')).toHaveTextContent(
    /^\/plan-your-trip$/,
  );
  expect(localStorage.getItem(draftKey)).toBeNull();
  expect(localStorage.getItem(languageStorageKey)).toBe('en');
});
