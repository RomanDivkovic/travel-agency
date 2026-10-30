import { render, screen } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { ErrorBoundary } from './ErrorBoundary';

afterEach(() => vi.restoreAllMocks());

it('replaces a failed subtree with an accessible recovery message', () => {
  vi.spyOn(console, 'error').mockImplementation(() => {});
  function BrokenComponent(): never {
    throw new Error('Internal detail must not appear in the UI');
  }
  render(
    <ErrorBoundary>
      <BrokenComponent />
    </ErrorBoundary>,
  );
  expect(screen.getByRole('alert')).toHaveTextContent('Sidan kunde inte visas');
  expect(screen.getByRole('button', { name: 'Ladda om' })).toBeInTheDocument();
  expect(screen.queryByText(/Internal detail/)).not.toBeInTheDocument();
});
