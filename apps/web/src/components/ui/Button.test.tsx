import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { expect, it, vi } from 'vitest';
import { Button, ButtonLink } from './Button';

it('uses native disabled button behavior and avoids accidental form submission', async () => {
  const onClick = vi.fn();
  const user = userEvent.setup();
  render(
    <Button disabled onClick={onClick}>
      Disabled action
    </Button>,
  );
  const button = screen.getByRole('button', { name: 'Disabled action' });
  expect(button).toHaveAttribute('type', 'button');
  await user.click(button);
  expect(onClick).not.toHaveBeenCalled();
});

it('uses a semantic anchor for navigation actions', () => {
  render(
    <MemoryRouter>
      <ButtonLink to="/plan-your-trip">Navigation action</ButtonLink>
    </MemoryRouter>,
  );
  expect(
    screen.getByRole('link', { name: 'Navigation action' }),
  ).toHaveAttribute('href', '/plan-your-trip');
});
