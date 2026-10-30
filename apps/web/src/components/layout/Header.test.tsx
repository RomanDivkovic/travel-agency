import { fireEvent, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { expect, it } from 'vitest';
import { sv } from '../../content/sv';
import { Header } from './Header';

function renderHeader() {
  return render(
    <MemoryRouter>
      <Header />
    </MemoryRouter>,
  );
}

it('exposes menu state and closes after selecting a navigation link', async () => {
  const user = userEvent.setup();
  renderHeader();
  const trigger = screen.getByRole('button', { name: sv.navigation.open });
  expect(trigger).toHaveAttribute('aria-expanded', 'false');
  expect(
    screen.queryByRole('navigation', { name: sv.navigation.mobileLabel }),
  ).not.toBeInTheDocument();
  await user.click(trigger);
  expect(trigger).toHaveAttribute('aria-expanded', 'true');
  const menu = screen.getByRole('navigation', {
    name: sv.navigation.mobileLabel,
  });
  await user.click(within(menu).getByRole('link', { name: 'Tjänster' }));
  expect(trigger).toHaveAttribute('aria-expanded', 'false');
});

it('supports keyboard opening, link focus, and Escape focus restoration', async () => {
  const user = userEvent.setup();
  renderHeader();
  const trigger = screen.getByRole('button', { name: sv.navigation.open });
  trigger.focus();
  await user.keyboard('{Enter}');
  await user.tab();
  const menu = screen.getByRole('navigation', {
    name: sv.navigation.mobileLabel,
  });
  expect(within(menu).getByRole('link', { name: 'Hem' })).toHaveFocus();
  await user.keyboard('{Escape}');
  expect(trigger).toHaveAttribute('aria-expanded', 'false');
  expect(trigger).toHaveFocus();
});

it('clears an open mobile menu when the viewport is resized', async () => {
  const user = userEvent.setup();
  renderHeader();
  const trigger = screen.getByRole('button', { name: sv.navigation.open });
  await user.click(trigger);
  fireEvent(window, new Event('resize'));
  expect(trigger).toHaveAttribute('aria-expanded', 'false');
});

it('closes the menu on an outside pointer interaction', async () => {
  const user = userEvent.setup();
  renderHeader();
  const trigger = screen.getByRole('button', { name: sv.navigation.open });
  await user.click(trigger);
  fireEvent.pointerDown(document.body);
  expect(trigger).toHaveAttribute('aria-expanded', 'false');
});
