import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { LanguageProvider } from '../../localization/LanguageProvider';
import { ApiError } from '../../services/apiClient';
import { CustomerAuthForm, CustomerTokenPage } from './AuthPages';
import { ClaimRequestForm } from './CustomerDashboardPage';
import { CustomerSessionContext, safeCustomerReturn } from './context';
import { customerAuthApi, customerPortalApi } from './api';
import { JourneyProgress } from './PortalShared';
import type { CustomerUser } from './types';
vi.mock('./api', () => ({
  customerAuthApi: {
    register: vi.fn(),
    login: vi.fn(),
    verify: vi.fn(),
    reset: vi.fn(),
    emailAction: vi.fn(),
  },
  customerPortalApi: { claim: vi.fn(), confirmClaim: vi.fn() },
}));
const user: CustomerUser = {
  id: 'customer',
  firstName: 'Test',
  lastName: 'Customer',
  email: 'test@example.test',
  phone: null,
  preferredLanguage: 'en',
  lastLoginAt: null,
  sessionExpiresAt: null,
};
function mount(node: React.ReactNode, route = '/login', signedIn = true) {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <LanguageProvider>
        <CustomerSessionContext
          value={{
            user: signedIn ? user : null,
            failed: false,
            setUser: vi.fn(),
            refresh: vi.fn(),
          }}
        >
          {node}
        </CustomerSessionContext>
      </LanguageProvider>
    </MemoryRouter>,
  );
}
beforeEach(() => {
  vi.resetAllMocks();
  localStorage.clear();
  sessionStorage.clear();
  localStorage.setItem('travel-agency.language.v1', 'en');
});
describe('safe customer navigation', () => {
  it.each([
    'https://evil.example',
    '//evil.example',
    '/admin',
    '/account/../admin',
    '/account?redirect=https://evil.example',
    undefined,
  ])('rejects unsafe destination %s', (value) =>
    expect(safeCustomerReturn(value)).toBe('/account'),
  );
  it('preserves an owned-resource-shaped return path', () =>
    expect(
      safeCustomerReturn(
        '/account/requests/12345678-1234-1234-1234-123456789abc',
      ),
    ).toContain('/account/requests/'));
});
describe('account forms', () => {
  it('keeps registration neutral and clears passwords after server confirmation', async () => {
    vi.mocked(customerAuthApi.register).mockResolvedValue(undefined);
    mount(<CustomerAuthForm mode="register" />);
    const u = userEvent.setup();
    await u.type(screen.getByLabelText('First name'), 'Test');
    await u.type(screen.getByLabelText('Last name'), 'Customer');
    await u.type(screen.getByLabelText('Email address'), user.email);
    await u.type(
      screen.getByLabelText('Password', { exact: true }),
      'TEST-password!7Aa',
    );
    await u.type(
      screen.getByLabelText('Confirm password'),
      'TEST-password!7Aa',
    );
    await u.click(screen.getByRole('button', { name: 'Create account' }));
    expect(await screen.findByRole('status')).toHaveTextContent(
      'Registration received',
    );
    expect(customerAuthApi.register).toHaveBeenCalledOnce();
    expect(JSON.stringify(localStorage)).not.toContain('TEST-password');
    expect(JSON.stringify(sessionStorage)).not.toContain(user.email);
  });
  it('does not claim verification on failed delivery configuration', async () => {
    vi.mocked(customerAuthApi.emailAction).mockRejectedValue(
      new ApiError('unavailable', 412),
    );
    mount(<CustomerAuthForm mode="forgot-password" />);
    const u = userEvent.setup();
    await u.type(screen.getByLabelText('Email address'), user.email);
    await u.click(screen.getByRole('button', { name: 'Reset password' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Account email is temporarily unavailable',
    );
    expect(screen.queryByText(/email has been queued/)).not.toBeInTheDocument();
  });
  it('does not consume verification links before an explicit confirmation', async () => {
    const token = 'A'.repeat(43);
    vi.mocked(customerAuthApi.verify).mockResolvedValue(undefined);
    mount(<CustomerTokenPage action="verify" />, '/verify-email#' + token);
    expect(customerAuthApi.verify).not.toHaveBeenCalled();
    expect(JSON.stringify(localStorage)).not.toContain(token);
    await userEvent.click(screen.getByRole('button', { name: 'Verify email' }));
    await waitFor(() =>
      expect(customerAuthApi.verify).toHaveBeenCalledWith(token),
    );
    expect(await screen.findByRole('status')).toHaveTextContent(
      'Email verified',
    );
  });
  it('requires sign in before confirming a request link', () => {
    mount(
      <CustomerTokenPage action="claim" />,
      '/confirm-request#' + 'A'.repeat(43),
      false,
    );
    expect(screen.getByText(/Sign in with the account/)).toBeVisible();
    expect(
      screen.queryByRole('button', { name: 'Link this request to my account' }),
    ).not.toBeInTheDocument();
    expect(customerPortalApi.confirmClaim).not.toHaveBeenCalled();
  });
  it('never claims ownership when the server only queues an eligible email', async () => {
    vi.mocked(customerPortalApi.claim).mockResolvedValue(undefined);
    mount(<ClaimRequestForm />);
    await userEvent.type(
      screen.getByLabelText('Travel request reference'),
      'TRV-TEST',
    );
    await userEvent.click(
      screen.getByRole('button', { name: 'Request secure association link' }),
    );
    expect(await screen.findByRole('status')).toHaveTextContent(
      'If this request is eligible',
    );
    expect(customerPortalApi.confirmClaim).not.toHaveBeenCalled();
  });
  it('keeps a failed claim retryable and does not display a success message', async () => {
    vi.mocked(customerPortalApi.claim).mockRejectedValue(
      new ApiError('network'),
    );
    mount(<ClaimRequestForm />);
    await userEvent.type(
      screen.getByLabelText('Travel request reference'),
      'TRV-TEST',
    );
    await userEvent.click(
      screen.getByRole('button', { name: 'Request secure association link' }),
    );
    expect(await screen.findByRole('alert')).toBeVisible();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });
  it('shows a partial booking as requiring contact without completing booking confirmation', () => {
    mount(
      <JourneyProgress
        journey={{
          stage: 'BookingPartiallyConfirmed',
          completedStages: ['InquirySubmitted', 'BookingInProgress'],
          nextAction: 'ContactAgent',
        }}
      />,
    );
    expect(screen.getByRole('status')).toHaveTextContent(
      'Booking partially confirmed',
    );
    expect(
      screen.getByRole('link', { name: 'Contact the travel agent' }),
    ).toHaveAttribute('href', '/contact');
  });
});
