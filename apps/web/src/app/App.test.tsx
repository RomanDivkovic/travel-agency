import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { beforeEach, expect, it, vi } from 'vitest';
import { getApiHealth } from '../services/health';
import { App } from './App';

vi.mock('../services/health', () => ({ getApiHealth: vi.fn() }));

beforeEach(() => {
  vi.mocked(getApiHealth).mockResolvedValue(undefined);
});

function renderRoute(path = '/') {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  );
}

it('renders the landing route and connected API status', async () => {
  renderRoute();
  expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
    'applikation körs',
  );
  expect(await screen.findByText('API anslutet')).toBeInTheDocument();
});

it('reports an unavailable API without crashing the application', async () => {
  vi.mocked(getApiHealth).mockRejectedValue(new Error('offline'));
  renderRoute();
  expect(await screen.findByText('API är inte tillgängligt')).toHaveAttribute(
    'role',
    'status',
  );
});

it('shows a checking state while the request is pending', () => {
  vi.mocked(getApiHealth).mockImplementation(() => new Promise(() => {}));
  renderRoute();
  expect(screen.getByRole('status')).toHaveTextContent('Kontrollerar API');
});

it('renders a not-found route with a way home', () => {
  renderRoute('/missing');
  expect(screen.getByRole('heading')).toHaveTextContent('Sidan hittades inte');
  expect(screen.getByRole('link', { name: 'Till startsidan' })).toHaveAttribute(
    'href',
    '/',
  );
  expect(getApiHealth).not.toHaveBeenCalled();
});
