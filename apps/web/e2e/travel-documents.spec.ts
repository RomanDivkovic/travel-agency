import { test, expect, type Page } from '@playwright/test';
import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';
const directory = process.env.TRAVEL_E2E_DIRECTORY!;
test.skip(
  process.env.TRAVEL_DOCUMENT_E2E !== '1',
  'Phase 11 isolated fixture required.',
);
async function english(page: Page) {
  await page
    .getByRole('combobox', { name: /^(Language|Språk)$/ })
    .selectOption('en');
}
async function mail(email: string, route: string) {
  let link = '';
  await expect
    .poll(
      async () => {
        for (const name of await readdir(path.join(directory, 'mail')).catch(
          () => [],
        )) {
          if (!name.endsWith('.json')) continue;
          const m: { to: string; text: string } = JSON.parse(
            await readFile(path.join(directory, 'mail', name), 'utf8'),
          );
          const found =
            m.to === email &&
            m.text.match(
              new RegExp(
                'http://localhost:5177/' + route + '#[A-Za-z0-9_-]{43}',
              ),
            );
          if (found) link = found[0];
        }
        return link;
      },
      { timeout: 30000 },
    )
    .not.toBe('');
  return link;
}
async function confirm(page: Page, name: string) {
  const button = page.getByRole('button', { name, exact: true });
  await button.click();
  await expect(
    page.getByRole('button', { name: 'Close preview', exact: true }),
  ).toBeVisible();
  await expect(button).toBeFocused();
  await button.press('Enter');
  await expect(
    page.getByRole('button', { name: 'Close preview', exact: true }),
  ).toHaveCount(0);
}
test('private documents and controlled itineraries reach only the verified owner', async ({
  browser,
}) => {
  const f: {
    documentEmail: string;
    documentBookingId: string;
    documentReference: string;
    bookingId: string;
  } = JSON.parse(await readFile(path.join(directory, 'fixture.json'), 'utf8'));
  const adminContext = await browser.newContext();
  const customerContext = await browser.newContext();
  const admin = await adminContext.newPage();
  const customer = await customerContext.newPage();
  const errors: string[] = [];
  for (const p of [admin, customer]) {
    p.on('pageerror', (e) => errors.push(e.message));
    p.on('response', (r) => {
      if (r.status() >= 500)
        errors.push(r.status() + ' ' + new URL(r.url()).pathname);
    });
    p.on('requestfailed', (r) => {
      if (r.failure()?.errorText !== 'net::ERR_ABORTED')
        errors.push(new URL(r.url()).pathname + ' ' + r.failure()?.errorText);
    });
  }
  await admin.goto('/admin/login');
  await english(admin);
  await admin
    .getByLabel('Email address', { exact: true })
    .fill(process.env.TRAVEL_E2E_ADMIN_EMAIL!);
  await admin
    .getByLabel('Password', { exact: true })
    .fill(process.env.TRAVEL_E2E_ADMIN_PASSWORD!);
  await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(admin).not.toHaveURL(/\/admin\/login$/);
  const adminBase = '/admin/bookings/' + f.documentBookingId;
  const customerBase = '/account/bookings/' + f.documentBookingId;
  await admin.goto(adminBase + '/documents');
  await admin
    .getByLabel('Display name', { exact: true })
    .fill('TEST flight confirmation');
  await admin
    .getByLabel('Document category')
    .selectOption('FlightConfirmation');
  await admin.getByLabel('Associated reservation').selectOption({ index: 1 });
  await admin
    .getByLabel('File (PDF, JPEG or PNG)')
    .setInputFiles(path.join(directory, 'fixture.pdf'));
  await admin.getByRole('checkbox').check();
  await admin
    .getByRole('button', { name: 'Upload document', exact: true })
    .click();
  await expect(admin.getByText('Scan clean', { exact: true })).toBeVisible();
  await confirm(admin, 'Publish');
  await expect(
    admin.getByText('Published', { exact: true }).first(),
  ).toBeVisible();
  await admin.getByRole('button', { name: 'Preview', exact: true }).click();
  await expect(admin.locator('iframe')).toBeVisible();
  await admin.getByRole('button', { name: 'Close preview' }).click();
  await customer.goto('/register');
  await english(customer);
  await customer.getByLabel('First name', { exact: true }).fill('Document');
  await customer.getByLabel('Last name', { exact: true }).fill('Customer');
  await customer
    .getByLabel('Email address', { exact: true })
    .fill(f.documentEmail);
  for (const label of ['Password', 'Confirm password'])
    await customer
      .getByLabel(label, { exact: true })
      .fill('TEST-browser-password!7Aa');
  await customer.getByLabel('Preferred language').selectOption('en');
  await customer
    .getByRole('button', { name: 'Create account', exact: true })
    .click();
  await expect(customer.getByRole('status')).toContainText(
    'Registration received',
  );
  await customer.goto(await mail(f.documentEmail, 'verify-email'));
  await customer
    .getByRole('button', { name: 'Verify email', exact: true })
    .click();
  await expect(customer.getByRole('status')).toContainText('Email verified');
  await customer.goto('/login');
  await customer
    .getByLabel('Email address', { exact: true })
    .fill(f.documentEmail);
  await customer
    .getByLabel('Password', { exact: true })
    .fill('TEST-browser-password!7Aa');
  await customer.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(customer).toHaveURL(/\/account$/);
  // Booking reference is distinct from the travel request reference; read the approved fixture's request reference.
  const data: { documentRequestReference: string } = JSON.parse(
    await readFile(path.join(directory, 'fixture.json'), 'utf8'),
  );
  await customer
    .getByLabel('Travel request reference', { exact: true })
    .fill(data.documentRequestReference);
  await customer
    .getByRole('button', { name: 'Request secure association link' })
    .click();
  await customer.goto(await mail(f.documentEmail, 'confirm-request'));
  await customer
    .getByRole('button', { name: 'Link this request to my account' })
    .click();
  await expect(customer.getByRole('status')).toContainText(
    'Request association confirmed',
  );
  await customer.goto(customerBase + '/documents');
  await expect(
    customer.getByText('TEST flight confirmation', { exact: true }),
  ).toBeVisible();
  const file = customer.waitForEvent('download');
  await customer.getByRole('button', { name: 'Download', exact: true }).click();
  const download = await file;
  const location = await download.path();
  expect(await readFile(location!)).toEqual(
    await readFile(path.join(directory, 'fixture.pdf')),
  );
  await expect(
    customer.getByRole('button', { name: 'Upload document', exact: true }),
  ).toHaveCount(0);
  await admin.goto(adminBase + '/itinerary');
  await admin
    .getByRole('button', { name: 'Create itinerary', exact: true })
    .click();
  await confirm(admin, 'Generate from confirmed bookings');
  await expect(
    admin.getByText('TEST-GENUINE-FIXTURE-REF').first(),
  ).toBeVisible();
  await admin
    .getByLabel('Trip title', { exact: true })
    .fill('TEST personal Lisbon itinerary');
  await admin.getByRole('button', { name: 'Add informational entry' }).click();
  await admin
    .getByLabel('Entry title', { exact: true })
    .fill('TEST airport guidance');
  await admin.getByLabel('Local travel date').fill('2027-06-01');
  await admin.getByRole('button', { name: 'Save draft', exact: true }).click();
  await expect(
    admin.getByText('TEST airport guidance', { exact: false }).last(),
  ).toBeVisible();
  await admin
    .getByRole('button', { name: 'Customer preview', exact: true })
    .click();
  await expect(admin.getByLabel('Trip title', { exact: true })).toHaveCount(0);
  await admin
    .getByRole('button', { name: 'Customer preview', exact: true })
    .click();
  await confirm(admin, 'Publish');
  await expect(
    admin.getByRole('button', { name: 'Create revision' }),
  ).toBeVisible();
  await expect
    .poll(
      async () => {
        const found = new Set<string>();
        for (const name of await readdir(path.join(directory, 'mail'))) {
          if (!name.endsWith('.json')) continue;
          const m: { to: string; text: string } = JSON.parse(
            await readFile(path.join(directory, 'mail', name), 'utf8'),
          );
          if (m.to === f.documentEmail)
            for (const resource of ['documents', 'itinerary'])
              if (m.text.includes(customerBase + '/' + resource))
                found.add(resource);
        }
        return found.size;
      },
      { timeout: 30000 },
    )
    .toBe(2);
  await customer.goto(customerBase + '/itinerary');
  await expect(
    customer.getByText('TEST personal Lisbon itinerary', { exact: true }),
  ).toBeVisible();
  await expect(
    customer.getByText('TEST airport guidance', { exact: false }),
  ).toBeVisible();
  const pdfEvent = customer.waitForEvent('download');
  await customer
    .getByRole('button', { name: 'Download itinerary PDF' })
    .click();
  const pdf = await pdfEvent;
  await pdf.saveAs(path.join(directory, 'customer-itinerary.pdf'));
  expect(
    (await readFile(path.join(directory, 'customer-itinerary.pdf')))
      .subarray(0, 5)
      .toString(),
  ).toBe('%PDF-');
  await confirm(admin, 'Create revision');
  await expect(
    admin.getByLabel('Trip title', { exact: true }).first(),
  ).toHaveValue('TEST personal Lisbon itinerary');
  await admin
    .getByLabel('Trip title', { exact: true })
    .first()
    .fill('TEST private revision');
  await admin.getByRole('button', { name: 'Save draft', exact: true }).click();
  await customer.reload();
  await expect(
    customer.getByText('TEST personal Lisbon itinerary', { exact: true }),
  ).toBeVisible();
  await expect(
    customer.getByText('TEST private revision', { exact: true }),
  ).toHaveCount(0);
  for (const language of ['en', 'sv']) {
    for (const p of [admin, customer])
      await p
        .getByRole('combobox', { name: /^(Language|Språk)$/ })
        .selectOption(language);
    for (const width of [375, 768, 1024, 1440]) {
      for (const [p, base] of [
        [admin, adminBase],
        [customer, customerBase],
      ] as const) {
        await p.setViewportSize({ width, height: 900 });
        for (const suffix of ['/documents', '/itinerary']) {
          await p.goto(base + suffix);
          await expect(
            p.locator('h1'),
            language +
              ' ' +
              width +
              ' ' +
              base +
              suffix +
              ' ' +
              JSON.stringify(errors),
          ).toBeVisible();
          await expect(p.getByText(/^(Loading…|Laddar…)$/)).toHaveCount(0);
          await expect(p.getByRole('alert')).toHaveCount(0);
          expect(
            await p.evaluate(
              () => document.documentElement.scrollWidth <= innerWidth,
            ),
            language + ' ' + width + ' ' + base + suffix,
          ).toBe(true);
          if (process.env.TRAVEL_AXE_PATH) {
            await p.addScriptTag({ path: process.env.TRAVEL_AXE_PATH });
            const result: { violations: { id: string }[] } = await p.evaluate(
              'axe.run(document,{runOnly:{type:"tag",values:["wcag2a","wcag2aa","wcag21aa","wcag22aa"]}})',
            );
            expect(result.violations.map((v) => v.id)).toEqual([]);
          }
          await p.screenshot({
            path: path.join(
              directory,
              `documents-${p === admin ? 'admin' : 'customer'}-${suffix.slice(1)}-${language}-${width}.png`,
            ),
            fullPage: true,
          });
        }
      }
    }
  }
  await customer.goto('/account/bookings/' + f.bookingId + '/documents');
  await expect(customer.getByRole('alert')).toBeVisible();
  expect(errors).toEqual([]);
  await adminContext.close();
  await customerContext.close();
});
