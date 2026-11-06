import { test, expect, type Page } from '@playwright/test';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
test.setTimeout(240000);
const directory = process.env.TRAVEL_E2E_DIRECTORY!;
test.skip(
  process.env.TRAVEL_CMS_E2E !== '1',
  'Phase 13 isolated fixture required.',
);
async function english(p: Page) {
  await p
    .getByRole('combobox', { name: /^(Language|Språk)$/ })
    .first()
    .selectOption('en');
  await expect(p.locator('html')).toHaveAttribute('lang', 'en');
}
async function save(p: Page) {
  await p.getByRole('button', { name: 'Save draft', exact: true }).focus();
  await p.keyboard.press('Enter');
  await expect(
    p.getByText('Draft saved. It has not been published.', { exact: true }),
  ).toBeVisible();
}
async function publish(p: Page) {
  await p
    .getByLabel('I have reviewed Swedish and English and want to publish this.')
    .check();
  const response = p.waitForResponse(
    (r) => r.url().endsWith('/publish') && r.request().method() === 'POST',
  );
  await p.getByRole('button', { name: 'Publish', exact: true }).click();
  expect((await response).status()).toBe(204);
  await expect(p.getByText('Change saved.', { exact: true })).toBeVisible();
}
test('administrator publishes reviewed business content while financial snapshots stay intact', async ({
  browser,
}) => {
  const f: { invoiceId: string; messagingEmail: string } = JSON.parse(
    await readFile(path.join(directory, 'fixture.json'), 'utf8'),
  );
  const adminContext = await browser.newContext();
  const visitorContext = await browser.newContext();
  const admin = await adminContext.newPage(),
    visitor = await visitorContext.newPage();
  const errors: string[] = [];
  for (const p of [admin, visitor]) {
    p.on('pageerror', (e) => errors.push(e.message));
    p.on('response', (r) => {
      if (r.status() >= 500)
        errors.push(r.status() + ' ' + new URL(r.url()).pathname);
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
  await expect(
    admin.getByRole('heading', { name: 'Dashboard', exact: true }),
  ).toBeVisible();
  const api = 'http://localhost:5084';
  const invoiceBefore = await (
    await admin.request.get(api + '/api/admin/invoices/' + f.invoiceId)
  ).json();
  const quoteBefore = await (
    await admin.request.get(
      api + '/api/admin/quotations/' + invoiceBefore.quotationId,
    )
  ).json();
  await admin.goto('/admin/settings/business');
  await admin
    .getByLabel('Contact email', { exact: true })
    .fill('cms-browser@example.test');
  await admin.getByLabel('Phone', { exact: true }).fill('+46 1234567');
  await admin.getByRole('button', { name: 'Add time range' }).click();
  await save(admin);
  await visitor.goto('/contact');
  await english(visitor);
  await expect(
    visitor.getByRole('link', { name: 'cms-browser@example.test' }),
  ).toHaveCount(0);
  await publish(admin);
  await visitor.reload();
  await expect(
    visitor.getByRole('link', { name: 'cms-browser@example.test' }).first(),
  ).toBeVisible();
  await expect(
    visitor.getByText('Opening hours — Europe/Stockholm'),
  ).toBeVisible();
  await visitor.goto('/');
  const png = await visitor.screenshot({ type: 'png' });
  await admin.goto('/admin/content/media');
  await admin
    .getByLabel('Image file (JPEG, PNG or WebP, up to 3 MB)')
    .setInputFiles({
      name: 'TEST-website.png',
      mimeType: 'image/png',
      buffer: png,
    });
  await admin
    .getByLabel('Swedish alternative text', { exact: true })
    .fill('TEST webbplatsbild');
  await admin
    .getByLabel('English alternative text', { exact: true })
    .fill('TEST website image');
  const uploadResponse = admin.waitForResponse(
    (r) =>
      r.url().endsWith('/api/admin/cms/media') &&
      r.request().method() === 'POST',
  );
  await admin.getByRole('button', { name: 'Upload image' }).click();
  const uploaded = await uploadResponse;
  expect(uploaded.status(), await uploaded.text()).toBe(200);
  await expect(
    admin.getByRole('heading', { name: 'TEST-website.png' }),
  ).toBeVisible();
  await admin.goto('/admin/content/edit/' + encodeURIComponent('page:about'));
  await admin
    .getByLabel('Section image', { exact: true })
    .selectOption({ label: 'TEST-website.png' });
  await save(admin);
  await admin.getByRole('link', { name: 'Preview saved draft' }).click();
  await expect(
    admin.getByRole('img', { name: 'TEST website image' }),
  ).toBeVisible();
  await visitor.goto('/about');
  await expect(
    visitor.getByRole('img', { name: 'TEST website image' }),
  ).toHaveCount(0);
  await admin.getByRole('link', { name: 'Back', exact: true }).click();
  await publish(admin);
  await visitor.reload();
  await expect(
    visitor.getByRole('img', { name: 'TEST website image' }),
  ).toBeVisible();
  await admin.goto(
    '/admin/content/edit/' + encodeURIComponent('service:FlightsOnly'),
  );
  await admin.getByLabel('Content language').selectOption('en');
  await admin
    .getByLabel('Service name', { exact: true })
    .fill('TEST reviewed flight assistance');
  await admin
    .getByLabel('Short description', { exact: true })
    .fill('TEST published service description');
  await admin.getByLabel('Content language').selectOption('sv');
  await admin
    .getByLabel('Service name', { exact: true })
    .fill('TEST granskad flyghjälp');
  await save(admin);
  await admin.getByRole('link', { name: 'Preview saved draft' }).click();
  await expect(
    admin.getByRole('heading', { name: 'TEST reviewed flight assistance' }),
  ).toBeVisible();
  await visitor.goto('/services');
  await expect(
    visitor.getByRole('heading', { name: 'TEST reviewed flight assistance' }),
  ).toHaveCount(0);
  await admin.getByRole('link', { name: 'Back', exact: true }).click();
  await publish(admin);
  await visitor.reload();
  await expect(
    visitor.getByRole('heading', { name: 'TEST reviewed flight assistance' }),
  ).toBeVisible();
  await admin.goto('/admin/content/edit/' + encodeURIComponent('page:home'));
  await admin.getByLabel('Content language').selectOption('en');
  await admin
    .locator('summary')
    .filter({ hasText: /^Hero$/ })
    .click();
  await admin
    .getByLabel('Hero · Title · 1', { exact: true })
    .fill('TEST next journey');
  await admin.getByLabel('Content language').selectOption('sv');
  await admin
    .getByLabel('Hero · Title · 1', { exact: true })
    .fill('TEST nästa resa');
  await save(admin);
  await admin.getByRole('link', { name: 'Preview saved draft' }).click();
  await expect(admin.getByRole('heading', { level: 1 })).toContainText(
    'TEST next journey',
  );
  await admin
    .getByRole('combobox', { name: 'Language', exact: true })
    .first()
    .selectOption('sv');
  await expect(admin.getByRole('heading', { level: 1 })).toContainText(
    'TEST nästa resa',
  );
  await english(admin);
  await admin.getByRole('link', { name: 'Back', exact: true }).click();
  await publish(admin);
  await visitor.goto('/');
  await expect(visitor.getByRole('heading', { level: 1 })).toContainText(
    'TEST next journey',
  );
  await admin.goto('/admin/content/faqs');
  await admin.getByRole('button', { name: 'New FAQ' }).click();
  await admin.getByLabel('Enabled', { exact: true }).check();
  await admin.getByLabel('Content language').selectOption('en');
  await admin
    .getByLabel('Question', { exact: true })
    .fill('TEST CMS question in English?');
  await admin
    .getByLabel('Answer', { exact: true })
    .fill('TEST CMS answer — no reservation or payment.');
  await admin.getByLabel('Content language').selectOption('sv');
  await admin
    .getByLabel('Question', { exact: true })
    .fill('TEST CMS fråga på svenska?');
  await admin
    .getByLabel('Answer', { exact: true })
    .fill('TEST CMS svar — ingen bokning eller betalning.');
  await save(admin);
  await publish(admin);
  const faqRoute = new URL(admin.url()).pathname;
  await visitor.goto('/faq');
  await visitor
    .getByText('TEST CMS question in English?', { exact: true })
    .click();
  await expect(
    visitor.getByText('TEST CMS answer — no reservation or payment.'),
  ).toBeVisible();
  await admin.goto(
    '/admin/content/edit/' + encodeURIComponent('service:CompleteTrip'),
  );
  await admin.getByLabel('Pricing model').selectOption('FixedFee');
  await admin.getByLabel('Service fee', { exact: true }).fill('700.00');
  await save(admin);
  await publish(admin);
  await visitor.goto('/pricing');
  await expect(
    visitor.getByRole('cell').filter({ hasText: /700/ }),
  ).toBeVisible();
  const invoiceAfter = await (
    await admin.request.get(api + '/api/admin/invoices/' + f.invoiceId)
  ).json();
  expect(invoiceAfter.invoice).toEqual(invoiceBefore.invoice);
  const quoteAfter = await (
    await admin.request.get(
      api + '/api/admin/quotations/' + invoiceBefore.quotationId,
    )
  ).json();
  expect(quoteAfter.quotation).toEqual(quoteBefore.quotation);
  await admin.goto(
    '/admin/content/edit/' + encodeURIComponent('policy:Privacy'),
  );
  await admin.getByLabel('Content language').selectOption('en');
  await admin
    .getByLabel('Title', { exact: true })
    .fill('TEST privacy — not legal text');
  await admin
    .getByLabel('Policy text', { exact: true })
    .fill('TEST policy for isolated browser verification only. '.repeat(400));
  await admin.getByLabel('Content language').selectOption('sv');
  await admin
    .getByLabel('Title', { exact: true })
    .fill('TEST integritet — inte juridisk text');
  await admin
    .getByLabel('Policy text', { exact: true })
    .fill('TEST policy endast för isolerad verifiering. '.repeat(400));
  await admin
    .getByLabel('Reviewer name')
    .fill('TEST fixture reviewer, not actual legal approval');
  await admin
    .getByLabel('Review date')
    .fill(new Date().toISOString().slice(0, 10));
  await admin.getByLabel('Reviewed by a qualified legal professional').check();
  await save(admin);
  await publish(admin);
  await visitor.goto('/policies/Privacy');
  await expect(
    visitor.getByRole('heading', { name: 'TEST privacy — not legal text' }),
  ).toBeVisible();
  const routes = [
    '/admin/settings/business',
    '/admin/settings/services',
    '/admin/content/edit/' + encodeURIComponent('service:FlightsOnly'),
    '/admin/settings/preferences',
    '/admin/content',
    '/admin/content/edit/' + encodeURIComponent('page:home'),
    '/admin/content/faqs',
    '/admin/content/policies',
    '/admin/content/edit/' + encodeURIComponent('policy:Privacy'),
    faqRoute,
    '/admin/content/media',
  ];
  for (const language of ['en', 'sv']) {
    await admin.goto('/admin/settings/business');
    await admin
      .getByRole('combobox', { name: /^(Language|Språk)$/ })
      .selectOption(language);
    await expect(admin.locator('html')).toHaveAttribute('lang', language);
    await visitor.goto('/contact');
    await visitor
      .getByRole('combobox', { name: /^(Language|Språk)$/ })
      .selectOption(language);
    await expect(visitor.locator('html')).toHaveAttribute('lang', language);
    for (const width of [375, 768, 1024, 1440]) {
      await admin.setViewportSize({ width, height: 900 });
      for (const [i, route] of routes.entries()) {
        await admin.goto(route);
        await expect(admin.locator('html')).toHaveAttribute('lang', language);
        await expect(admin.locator('h1')).toBeVisible();
        await expect(admin.getByText(/^(Loading…|Laddar…)$/)).toHaveCount(0);
        await expect(admin.getByRole('alert')).toHaveCount(0);
        expect(
          await admin.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
          ),
          route + ' ' + width,
        ).toBe(true);
        if (process.env.TRAVEL_AXE_PATH) {
          await admin.addScriptTag({ path: process.env.TRAVEL_AXE_PATH });
          const result: { violations: { id: string }[] } = await admin.evaluate(
            'axe.run(document,{runOnly:{type:"tag",values:["wcag2a","wcag2aa","wcag21aa","wcag22aa"]}})',
          );
          expect(
            result.violations.map((v) => v.id),
            route + ' ' + language,
          ).toEqual([]);
        }
        await admin.screenshot({
          path: path.join(directory, `cms-admin-${i}-${language}-${width}.png`),
          fullPage: true,
        });
      }
      await visitor.setViewportSize({ width, height: 900 });
      for (const [i, route] of [
        '/',
        '/about',
        '/services',
        '/pricing',
        '/contact',
        '/faq',
        '/policies/Privacy',
      ].entries()) {
        const response = visitor.waitForResponse(
          (r) => new URL(r.url()).pathname === '/api/public/website',
        );
        await visitor.goto(route);
        expect((await response).status()).toBeLessThan(400);
        await expect(visitor.locator('html')).toHaveAttribute('lang', language);
        await expect(visitor.locator('h1')).toBeVisible();
        await expect(
          visitor
            .getByRole('link', { name: 'cms-browser@example.test' })
            .first(),
        ).toBeVisible();
        expect(
          await visitor.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
          ),
          route + ' ' + width,
        ).toBe(true);
        await visitor.evaluate(async () => {
          await document.fonts.ready;
          await Promise.all(
            document
              .getAnimations()
              .filter(
                (a) => a.effect?.getComputedTiming().iterations !== Infinity,
              )
              .map((a) => a.finished.catch(() => undefined)),
          );
        });
        if (process.env.TRAVEL_AXE_PATH) {
          await visitor.addScriptTag({ path: process.env.TRAVEL_AXE_PATH });
          const a: { violations: { id: string }[] } = await visitor.evaluate(
            'axe.run(document,{runOnly:{type:"tag",values:["wcag2a","wcag2aa","wcag21aa","wcag22aa"]}})',
          );
          expect(
            a.violations.map((v) => v.id),
            route + ' ' + language,
          ).toEqual([]);
        }
        await visitor.screenshot({
          path: path.join(
            directory,
            `cms-public-${i}-${language}-${width}.png`,
          ),
          fullPage: false,
        });
      }
    }
  }
  await visitor.goto('/login');
  await english(visitor);
  await visitor
    .getByLabel('Email address', { exact: true })
    .fill(f.messagingEmail);
  await visitor
    .getByLabel('Password', { exact: true })
    .fill(process.env.TRAVEL_E2E_MESSAGE_PASSWORD!);
  await visitor.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(visitor).toHaveURL(/\/account$/);
  await visitor.goto('/admin/settings/business');
  await expect(visitor).toHaveURL(/\/admin\/login/);
  expect(errors).toEqual([]);
  await adminContext.close();
  await visitorContext.close();
});
