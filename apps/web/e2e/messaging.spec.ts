import { test, expect, type Page } from '@playwright/test';
import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';
const directory = process.env.TRAVEL_E2E_DIRECTORY!;
test.skip(
  process.env.TRAVEL_MESSAGING_E2E !== '1',
  'Phase 12 isolated fixture required.',
);
async function english(p: Page) {
  await p
    .getByRole('combobox', { name: /^(Language|Språk)$/ })
    .selectOption('en');
}
async function capturedMessage(subject: string, route: string) {
  await expect
    .poll(
      async () => {
        for (const name of await readdir(path.join(directory, 'mail')).catch(
          () => [],
        )) {
          if (!name.endsWith('.json')) continue;
          const m: { subject: string; text: string } = JSON.parse(
            await readFile(path.join(directory, 'mail', name), 'utf8'),
          );
          if (m.text.includes(subject) && m.text.includes(route)) {
            expect(m.text).not.toContain('TEST private browser body');
            return true;
          }
        }
        return false;
      },
      { timeout: 30000 },
    )
    .toBe(true);
}
test('customer and agent exchange private messages without replacing formal workflows', async ({
  browser,
}) => {
  const f: {
    messagingEmail: string;
    messagingBookingId: string;
    messagingRequestId: string;
    otherConversationId: string;
  } = JSON.parse(await readFile(path.join(directory, 'fixture.json'), 'utf8'));
  const customerContext = await browser.newContext({
    viewport: { width: 375, height: 900 },
  });
  const adminContext = await browser.newContext();
  const customer = await customerContext.newPage(),
    admin = await adminContext.newPage();
  const errors: string[] = [];
  for (const p of [customer, admin]) {
    p.on('pageerror', (e) => errors.push(e.message));
    p.on('response', (r) => {
      if (r.status() >= 500)
        errors.push(r.status() + ' ' + new URL(r.url()).pathname);
    });
  }
  await customer.goto('/login');
  await english(customer);
  await customer
    .getByLabel('Email address', { exact: true })
    .fill(f.messagingEmail);
  await customer
    .getByLabel('Password', { exact: true })
    .fill(process.env.TRAVEL_E2E_MESSAGE_PASSWORD!);
  await customer.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(customer).toHaveURL(/\/account$/);
  await customer.goto('/account/requests/' + f.messagingRequestId);
  await customer
    .getByRole('link', { name: 'Contact your travel agent' })
    .click();
  await expect(customer.getByLabel('Conversation relates to')).toHaveValue(
    'requests',
  );
  await expect(customer.getByLabel('Related reference')).not.toHaveValue('');
  const subject =
    'TEST browser communication ' + f.messagingRequestId.slice(0, 8);
  await customer.getByLabel('Subject', { exact: true }).fill(subject);
  await customer
    .getByLabel('Message', { exact: true })
    .fill(
      'TEST private browser body <script>alert(1)</script>\nI approve this trip — discussion only.',
    );
  await customer
    .getByRole('button', { name: 'Send message', exact: true })
    .focus();
  await customer.keyboard.press('Enter');
  await expect(customer).toHaveURL(/\/account\/messages\/[0-9a-f-]{36}$/);
  const id = customer.url().split('/').at(-1)!;
  await expect(
    customer.getByRole('heading', { name: subject, exact: true }),
  ).toBeVisible();
  await expect(customer.getByText(/TEST private browser body/)).toBeVisible();
  expect(await customer.locator('main script').count()).toBe(0);
  await capturedMessage(subject, '/admin/messages/' + id);
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
  await admin.goto('/admin/messages');
  await admin.getByLabel('Search conversations').fill(subject);
  await admin.getByRole('button', { name: 'Apply filters' }).click();
  await expect(
    admin.getByRole('link', { name: subject, exact: true }),
  ).toBeVisible();
  await admin.getByLabel('Unread only').check();
  await admin.getByRole('button', { name: 'Apply filters' }).click();
  await expect(
    admin.getByText('1 unread messages', { exact: true }).last(),
  ).toBeVisible();
  await admin.getByRole('link', { name: subject, exact: true }).click();
  await expect(admin.getByText(/TEST private browser body/)).toBeVisible();
  await admin
    .getByLabel('Message', { exact: true })
    .fill(
      'TEST agent reply\nPlease use the formal approval page for approval.',
    );
  await admin
    .getByRole('button', { name: 'Send message', exact: true })
    .click();
  await expect(admin.getByRole('status')).toContainText(
    'Message saved by the server.',
  );
  await expect(admin.getByText(/TEST agent reply/)).toBeVisible();
  await capturedMessage(subject, '/account/messages/' + id);
  await customer.goto('/account/messages');
  await expect(
    customer.getByText('1 unread messages', { exact: true }).last(),
  ).toBeVisible();
  await customer.getByRole('link', { name: subject, exact: true }).click();
  await expect(customer.getByText(/TEST agent reply/)).toBeVisible();
  await expect
    .poll(async () => {
      const r = await customerContext.request.get(
        'http://localhost:5084/api/customer/conversations/unread-count',
      );
      expect(r.status()).toBe(200);
      return ((await r.json()) as { unreadCount: number }).unreadCount;
    })
    .toBe(0);
  await admin
    .getByLabel('Conversation status', { exact: true })
    .selectOption('Closed');
  const closedResponse = admin.waitForResponse(
    (r) =>
      r.url().endsWith('/conversations/' + id + '/status') &&
      r.request().method() === 'PATCH',
  );
  await admin
    .getByRole('button', { name: 'Save change', exact: true })
    .first()
    .click();
  expect((await closedResponse).status()).toBe(204);
  await expect(
    admin.getByText('Change saved by the server.', { exact: true }),
  ).toBeVisible();
  await customer.getByRole('button', { name: 'Refresh messages' }).click();
  await expect(
    customer.getByText('This conversation is closed.', { exact: false }),
  ).toBeVisible();
  await expect(
    customer.getByRole('button', { name: 'Send message', exact: true }),
  ).toHaveCount(0);
  await admin
    .getByLabel('Conversation status', { exact: true })
    .selectOption('Open');
  const openResponse = admin.waitForResponse(
    (r) =>
      r.url().endsWith('/conversations/' + id + '/status') &&
      r.request().method() === 'PATCH',
  );
  await admin
    .getByRole('button', { name: 'Save change', exact: true })
    .first()
    .click();
  expect((await openResponse).status()).toBe(204);
  await expect(admin.getByLabel('Conversation status')).toHaveValue('Open');
  await customer.getByRole('button', { name: 'Refresh messages' }).click();
  await customer
    .getByLabel('Message', { exact: true })
    .fill('TEST mobile follow-up');
  await customer
    .getByRole('button', { name: 'Send message', exact: true })
    .click();
  await expect(customer.getByRole('status')).toContainText(
    'Message saved by the server.',
  );
  await customer.goto('/account/bookings/' + f.messagingBookingId);
  await customer
    .getByRole('link', { name: 'Ask a question about this booking' })
    .click();
  await expect(customer.getByLabel('Conversation relates to')).toHaveValue(
    'bookings',
  );
  await expect(customer.getByLabel('Related reference')).not.toHaveValue('');
  for (const language of ['en', 'sv']) {
    for (const p of [customer, admin]) {
      await p
        .getByRole('combobox', { name: /^(Language|Språk)$/ })
        .selectOption(language);
      await expect(p.locator('html')).toHaveAttribute('lang', language);
    }
    for (const width of [375, 768, 1024, 1440]) {
      for (const [p, routes] of [
        [
          customer,
          [
            '/account/messages',
            '/account/messages/' + id,
            '/account/messages/new',
          ],
        ],
        [admin, ['/admin/messages', '/admin/messages/' + id]],
      ] as const) {
        await p.setViewportSize({ width, height: 900 });
        for (const route of routes) {
          await p.goto(route);
          await expect(p.locator('html')).toHaveAttribute('lang', language);
          await expect(p.locator('h1')).toBeVisible();
          await expect(p.getByText(/^(Loading…|Laddar…)$/)).toHaveCount(0);
          await expect(p.getByRole('alert')).toHaveCount(0);
          expect(
            await p.evaluate(
              () => document.documentElement.scrollWidth <= innerWidth,
            ),
            language + ' ' + width + ' ' + route,
          ).toBe(true);
          if (process.env.TRAVEL_AXE_PATH) {
            await p.addScriptTag({ path: process.env.TRAVEL_AXE_PATH });
            const a: { violations: { id: string }[] } = await p.evaluate(
              'axe.run(document,{runOnly:{type:"tag",values:["wcag2a","wcag2aa","wcag21aa","wcag22aa"]}})',
            );
            expect(a.violations.map((v) => v.id)).toEqual([]);
          }
          await p.screenshot({
            path: path.join(
              directory,
              `messages-${p === admin ? 'admin' : 'customer'}-${route.endsWith(id) ? 'thread' : route.endsWith('new') ? 'new' : 'inbox'}-${language}-${width}.png`,
            ),
            fullPage: true,
          });
        }
      }
    }
  }
  await customer.goto('/account/messages/' + f.otherConversationId);
  await expect(customer.getByRole('alert')).toBeVisible();
  await expect(customer.getByText('TEST private other customer')).toHaveCount(
    0,
  );
  expect(errors).toEqual([]);
  await customerContext.close();
  await adminContext.close();
});
