import { render, screen } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { LanguageContext } from '../localization/useLanguage';
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

it('localizes recovery when the selected public language is English', () => {
  vi.spyOn(console, 'error').mockImplementation(() => {});
  function Broken(): never {
    throw new Error('private');
  }
  render(
    <LanguageContext value={{ language: 'en', setLanguage: () => {} }}>
      <ErrorBoundary>
        <Broken />
      </ErrorBoundary>
    </LanguageContext>,
  );
  expect(screen.getByRole('alert')).toHaveTextContent(
    'The page could not be displayed',
  );
  expect(screen.getByRole('button', { name: 'Reload' })).toBeInTheDocument();
});
