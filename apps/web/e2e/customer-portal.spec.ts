import { test, expect } from '@playwright/test';
import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';
interface Fixture {
  email: string;
  requests: { id: string; reference: string }[];
  quotationId: string;
  proposalId: string;
  invoiceId: string;
  bookingId: string;
}
const directory = process.env.TRAVEL_E2E_DIRECTORY!;
async function mailLink(
  email: string,
  route: string,
  reference?: string,
): Promise<string> {
  let link = '';
  await expect
    .poll(
      async () => {
        for (const name of await readdir(path.join(directory, 'mail')).catch(
          () => [],
        )) {
          if (!name.endsWith('.json')) continue;
          const mail: { to: string; text: string } = JSON.parse(
            await readFile(path.join(directory, 'mail', name), 'utf8'),
          );
          if (
            mail.to === email &&
            (!reference || mail.text.includes(reference))
          ) {
            const found = mail.text.match(
              new RegExp(
                'http://localhost:5177/' + route + '#[A-Za-z0-9_-]{43}',
              ),
            );
            if (found) link = found[0];
          }
        }
        return Boolean(link);
      },
      { timeout: 30000 },
    )
    .toBe(true);
  return link;
}
test('verified customer claims guest records and uses the complete portal', async ({
  page,
}) => {
  const f: Fixture = JSON.parse(
    await readFile(path.join(directory, 'fixture.json'), 'utf8'),
  );
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));
  page.on('console', (m) => {
    if (m.type() === 'error' && !m.text().includes('401'))
      errors.push(m.text());
  });
  await page.goto('/plan-your-trip?service=flights');
  await page
    .getByRole('combobox', { name: 'Språk', exact: true })
    .selectOption('en');
  const next = () =>
    page.getByRole('button', { name: 'Continue', exact: true }).click();
  await next();
  await page.locator('#origin').fill('Stockholm');
  await page.locator('#place').fill('Lisbon');
  await next();
  const departure = new Date();
  departure.setUTCFullYear(departure.getUTCFullYear() + 1);
  const returning = new Date(departure);
  returning.setUTCDate(returning.getUTCDate() + 7);
  await page.locator('#departure').fill(departure.toISOString().slice(0, 10));
  await page.locator('#return').fill(returning.toISOString().slice(0, 10));
  await next();
  await page.locator('#adults').fill('2');
  await next();
  await page.locator('#amount').fill('25000');
  await next();
  await page.locator('#priority').selectOption('direct');
  await next();
  await page.locator('#special').fill('TEST browser guest inquiry');
  await next();
  await page.locator('#name').fill('Browser Customer');
  await page.locator('#email').fill(f.email);
  await next();
  const receipt = page.waitForResponse(
    (r) =>
      r.url().endsWith('/api/travel-requests') &&
      r.request().method() === 'POST',
  );
  await page.getByRole('button', { name: 'Send free enquiry' }).click();
  const response = await receipt;
  expect(response.status()).toBe(201);
  const submitted: { referenceNumber: string } = await response.json();
  f.requests.push({ id: '', reference: submitted.referenceNumber });
  await page.goto('/register');
  await page.getByLabel('First name', { exact: true }).fill('Browser');
  await page.getByLabel('Last name', { exact: true }).fill('Customer');
  await page.getByLabel('Email address', { exact: true }).fill(f.email);
  await page
    .getByLabel('Password', { exact: true })
    .fill('TEST-browser-password!7Aa');
  await page
    .getByLabel('Confirm password', { exact: true })
    .fill('TEST-browser-password!7Aa');
  await page.getByLabel('Preferred language').selectOption('en');
  await page
    .getByRole('button', { name: 'Create account', exact: true })
    .click();
  await expect(page.getByRole('status')).toContainText('Registration received');
  await page.goto(await mailLink(f.email, 'verify-email'));
  await expect(page).toHaveURL(/\/verify-email$/);
  await page.getByRole('button', { name: 'Verify email', exact: true }).click();
  await expect(page.getByRole('status')).toContainText('Email verified');
  await page.goto('/login');
  await page.getByLabel('Email address', { exact: true }).fill(f.email);
  await page
    .getByLabel('Password', { exact: true })
    .fill('TEST-browser-password!7Aa');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page).toHaveURL(/\/account$/);
  await expect(
    page.getByRole('heading', { name: 'Welcome, Browser' }),
  ).toBeVisible();
  await expect(
    page.getByRole('link', { name: f.requests[0]!.reference }),
  ).toHaveCount(0);
  for (const request of f.requests) {
    await page.goto('/account');
    await page
      .getByLabel('Travel request reference', { exact: true })
      .fill(request.reference);
    const names: string[] = await readdir(path.join(directory, 'mail')).catch(
      () => [],
    );
    await page
      .getByRole('button', { name: 'Request secure association link' })
      .click();
    await expect(page.getByRole('status')).toContainText(
      'If this request is eligible',
    );
    // Each claim email is identified by its new filename; the body deliberately contains no request PII.
    let claim = '';
    await expect
      .poll(
        async () => {
          for (const name of await readdir(path.join(directory, 'mail'))) {
            if (!name.endsWith('.json') || names.includes(name)) continue;
            const mail: { to: string; text: string } = JSON.parse(
              await readFile(path.join(directory, 'mail', name), 'utf8'),
            );
            const found =
              mail.to === f.email &&
              mail.text.match(
                /http:\/\/localhost:5177\/confirm-request#[A-Za-z0-9_-]{43}/,
              );
            if (found) claim = found[0];
          }
          return claim;
        },
        { timeout: 30000 },
      )
      .not.toBe('');
    await page.goto(claim);
    await expect(page).toHaveURL(/\/confirm-request$/);
    await page
      .getByRole('button', { name: 'Link this request to my account' })
      .click();
    await expect(page.getByRole('status')).toContainText(
      'Request association confirmed',
    );
  }
  await page.goto('/account/quotations/' + f.quotationId);
  await expect(
    page.getByRole('button', { name: 'Accept service quotation' }),
  ).toBeDisabled();
  await page.getByRole('checkbox').check();
  await page.getByRole('button', { name: 'Accept service quotation' }).click();
  await expect(page.getByRole('status')).toHaveText('Accepted');
  await page.goto('/account/proposals/' + f.proposalId);
  await page.getByRole('radio').first().check();
  await page.getByRole('checkbox').check();
  await page
    .getByRole('button', { name: 'Approve selected alternative' })
    .click();
  await page.getByRole('button', { name: 'Yes, confirm' }).click();
  await expect(
    page.getByText('Approved', { exact: true }).first(),
  ).toBeVisible();
  for (const route of [
    'requests',
    'quotations',
    'invoices',
    'proposals',
    'bookings',
    'security',
  ]) {
    await page.goto('/account/' + route);
    await expect(page.locator('h1')).toBeVisible();
    await expect(page.getByRole('alert')).toHaveCount(0);
  }
  await page.goto('/account/bookings/' + f.bookingId);
  await expect(
    page.getByText('TEST-GENUINE-FIXTURE-REF').first(),
  ).toBeVisible();
  await expect(page.locator('body')).not.toContainText(
    'TEST isolated supplier confirmation email',
  );
  await page.goto('/account/invoices/' + f.invoiceId);
  await expect(page.getByRole('alert')).toHaveCount(0);
  await page.goto('/account/profile');
  await page.getByLabel('First name', { exact: true }).fill('Updated');
  await page.getByRole('button', { name: 'Save', exact: true }).click();
  await expect(page.getByRole('status')).toContainText('Saved by the server');
  await page
    .getByRole('combobox', { name: 'Language', exact: true })
    .selectOption('sv');
  await expect(page.locator('html')).toHaveAttribute('lang', 'sv');
  await page.reload();
  await expect(page.locator('html')).toHaveAttribute('lang', 'sv');
  await page
    .getByRole('combobox', { name: 'Språk', exact: true })
    .selectOption('en');
  for (const language of ['en', 'sv']) {
    await page
      .getByRole('combobox', { name: /^(Language|Språk)$/ })
      .selectOption(language);
    await expect(page.locator('html')).toHaveAttribute('lang', language);
    for (const width of [375, 768, 1024, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      for (const route of [
        '/account',
        '/account/profile',
        '/account/requests/' + f.requests[0]!.id,
        '/account/bookings/' + f.bookingId,
      ]) {
        await page.goto(route);
        await expect(page.locator('h1')).toBeVisible();
        await expect(page.getByText(/^(Loading…|Laddar…)$/)).toHaveCount(0);
        if (process.env.TRAVEL_AXE_PATH) {
          await page.addScriptTag({ path: process.env.TRAVEL_AXE_PATH });
          const report: { violations: { id: string }[] } = await page.evaluate(
            'axe.run(document, { runOnly: { type: "tag", values: ["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"] } })',
          );
          expect(report.violations.map((v) => v.id)).toEqual([]);
        }

        expect(
          await page.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
          ),
          `${language} ${width}px ${route}`,
        ).toBe(true);
      }
      await page.screenshot({
        path: path.join(directory, `portal-${language}-${width}.png`),
        fullPage: true,
      });
    }
  }
  await page
    .getByRole('combobox', { name: /^(Language|Språk)$/ })
    .selectOption('en');
  await page.setViewportSize({ width: 375, height: 900 });
  await page.locator('summary').focus();
  await page.keyboard.press('Enter');
  await page
    .locator('details')
    .getByRole('link', { name: 'Profile', exact: true })
    .focus();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/account\/profile$/);
  await expect(page.locator('main')).toBeFocused();
  expect(
    await page.evaluate(() =>
      JSON.stringify({ ...localStorage, ...sessionStorage }),
    ),
  ).not.toContain('TEST-browser-password');
  await page.getByRole('button', { name: 'Sign out of all sessions' }).click();
  await expect(page).toHaveURL(/\/login$/);
  await page.goto('/account/bookings/' + f.bookingId);
  await expect(page).toHaveURL(/\/login$/);
  await page.goto('/forgot-password');
  await page.getByLabel('Email address', { exact: true }).fill(f.email);
  await page
    .getByRole('button', { name: 'Reset password', exact: true })
    .click();
  await expect(page.getByRole('status')).toContainText(
    'If the account is eligible',
  );
  await page.goto(await mailLink(f.email, 'reset-password'));
  await expect(page).toHaveURL(/\/reset-password$/);
  await page
    .getByLabel('New password', { exact: true })
    .fill('TEST-reset-password!8Bb');
  await page
    .getByLabel('Confirm password', { exact: true })
    .fill('TEST-reset-password!8Bb');
  await page
    .getByRole('button', { name: 'Reset password', exact: true })
    .click();
  await expect(page.getByRole('status')).toContainText('Password updated');
  expect(errors).toEqual([]);
});

test('public pages and account entry remain usable in both languages', async ({
  page,
}) => {
  for (const language of ['sv', 'en']) {
    await page.goto('/');
    await page.getByRole('combobox').selectOption(language);
    await expect(page.locator('html')).toHaveAttribute('lang', language);
    for (const width of [375, 768, 1024, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      for (const route of [
        '/',
        '/services',
        '/pricing',
        '/about',
        '/contact',
        '/faq',
        '/plan-your-trip',
        '/login',
        '/register',
      ]) {
        await page.goto(route);
        await expect(page.locator('h1')).toBeVisible();
        expect(
          await page.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
          ),
          `${language} ${width}px ${route}`,
        ).toBe(true);
        for (const img of await page.locator('img').all()) {
          if (!(await img.isVisible())) continue;
          await img.scrollIntoViewIfNeeded();
          await expect
            .poll(() =>
              img.evaluate(
                (el) =>
                  el instanceof HTMLImageElement &&
                  el.complete &&
                  el.naturalWidth > 0,
              ),
            )
            .toBe(true);
        }
      }
    }
  }
});
