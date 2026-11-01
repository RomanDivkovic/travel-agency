import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router';
import { beforeEach, expect, it, vi } from 'vitest';
import { LanguageProvider } from '../../localization/LanguageProvider';
import { languageStorageKey } from '../../localization/useLanguage';
import { AxiosHeaders } from 'axios';
import { ApiError, apiClient } from '../../services/apiClient';
import {
  submitTravelRequest,
  validReceipt,
} from '../../services/travelRequests';
import { App } from '../../app/App';
import { SubmitPanel } from './components/SubmitPanel';
import { SuccessPage } from './SuccessPage';
import { buildTravelRequest } from './payload';
import { validWizard } from './testData';
import {
  attemptKey,
  receiptKey,
  submissionAttempt,
  storedReceipt,
} from './submissionStorage';
import { draftKey, storeDraft } from './draft';
import { retryDeadline } from './useRequestSubmission';
vi.mock('../../services/travelRequests', async (original) => ({
  ...(await original<typeof import('../../services/travelRequests')>()),
  submitTravelRequest: vi.fn(),
}));
const receipt = {
  referenceNumber: 'TRV-2026-000123',
  status: 'submitted' as const,
  submittedAt: '2026-11-01T12:00:00Z',
};
const onValidation = vi.fn(),
  onBusy = vi.fn();
const payload = buildTravelRequest(validWizard('flights'), 'en')!;
beforeEach(() => {
  vi.mocked(submitTravelRequest).mockReset();
  onValidation.mockClear();
  onBusy.mockClear();
  localStorage.setItem(languageStorageKey, 'en');
});
function panel() {
  return render(
    <LanguageProvider>
      <MemoryRouter>
        <Routes>
          <Route
            path="/"
            element={
              <SubmitPanel
                payload={payload}
                onValidation={onValidation}
                onBusy={onBusy}
              />
            }
          />
          <Route path="/plan-your-trip/success" element={<SuccessPage />} />
        </Routes>
      </MemoryRouter>
    </LanguageProvider>,
  );
}
it('prevents duplicate clicks, shows loading, confirms only a saved receipt, and clears draft/key', async () => {
  storeDraft(validWizard(), true);
  let resolve!: (value: typeof receipt) => void;
  vi.mocked(submitTravelRequest).mockImplementation(
    () =>
      new Promise((done) => {
        resolve = done;
      }),
  );
  panel();
  const user = userEvent.setup();
  await user.dblClick(
    screen.getByRole('button', { name: 'Send free enquiry' }),
  );
  expect(submitTravelRequest).toHaveBeenCalledTimes(1);
  expect(screen.getByRole('button', { name: 'Sending…' })).toBeDisabled();
  expect(localStorage.getItem(draftKey)).not.toBeNull();
  expect(localStorage.getItem(attemptKey)).not.toBeNull();
  await act(async () => resolve(receipt));
  expect(await screen.findByRole('heading', { level: 1 })).toHaveTextContent(
    'We have received',
  );
  expect(screen.getByText(/TRV-2026-000123/)).toBeInTheDocument();
  expect(localStorage.getItem(draftKey)).toBeNull();
  expect(localStorage.getItem(attemptKey)).toBeNull();
  expect(sessionStorage.getItem(receiptKey)).not.toContain('anna@');
  expect(
    screen.getByText(/not that an email has been delivered/),
  ).toBeInTheDocument();
});
it('preserves the draft and same retry key after an uncertain network failure, including remount', async () => {
  storeDraft(validWizard(), true);
  vi.mocked(submitTravelRequest).mockRejectedValueOnce(new ApiError('safe'));
  const app = panel();
  await userEvent
    .setup()
    .click(screen.getByRole('button', { name: 'Send free enquiry' }));
  expect(await screen.findByRole('alert')).toHaveTextContent(
    'may have been saved',
  );
  const first = vi.mocked(submitTravelRequest).mock.calls[0]![1];
  expect(localStorage.getItem(draftKey)).not.toBeNull();
  app.unmount();
  vi.mocked(submitTravelRequest).mockResolvedValueOnce(receipt);
  panel();
  await userEvent
    .setup()
    .click(screen.getByRole('button', { name: 'Send free enquiry' }));
  await screen.findByRole('heading', { level: 1 });
  expect(vi.mocked(submitTravelRequest).mock.calls[1]![1]).toBe(first);
});
it('maps safe validation codes and distinguishes conflicts and server failures', async () => {
  vi.mocked(submitTravelRequest).mockRejectedValueOnce(
    new ApiError('safe', 400, { errors: { email: ['email'] } }),
  );
  const app = panel();
  await userEvent
    .setup()
    .click(screen.getByRole('button', { name: 'Send free enquiry' }));
  await waitFor(() =>
    expect(onValidation).toHaveBeenCalledWith({ email: 'email' }),
  );
  const key = submissionAttempt();
  app.unmount();
  vi.mocked(submitTravelRequest).mockRejectedValueOnce(
    new ApiError('safe', 409),
  );
  const conflict = panel();
  await userEvent
    .setup()
    .click(screen.getByRole('button', { name: 'Send free enquiry' }));
  expect(await screen.findByRole('alert')).toHaveTextContent(
    'different answers',
  );
  expect(submissionAttempt()).toBe(key);
  conflict.unmount();
  vi.mocked(submitTravelRequest).mockRejectedValueOnce(
    new ApiError('safe', 503),
  );
  panel();
  await userEvent
    .setup()
    .click(screen.getByRole('button', { name: 'Send free enquiry' }));
  expect(await screen.findByRole('alert')).toHaveTextContent(
    'could not be confirmed',
  );
  expect(submissionAttempt()).toBe(key);
});
it('respects Retry-After before enabling retry and retains the same key', async () => {
  vi.mocked(submitTravelRequest)
    .mockRejectedValueOnce(new ApiError('safe', 429, undefined, '1'))
    .mockResolvedValueOnce(receipt);
  panel();
  await userEvent
    .setup()
    .click(screen.getByRole('button', { name: 'Send free enquiry' }));
  expect(await screen.findByRole('alert')).toHaveTextContent(
    'Too many attempts',
  );
  const button = screen.getByRole('button', { name: 'Retry the same enquiry' });
  expect(button).toBeDisabled();
  await waitFor(() => expect(button).toBeEnabled(), { timeout: 3000 });
  await userEvent.setup().click(button);
  await screen.findByRole('heading', { level: 1 });
  expect(vi.mocked(submitTravelRequest).mock.calls[0]![1]).toBe(
    vi.mocked(submitTravelRequest).mock.calls[1]![1],
  );
});
it('does not submit if recovery storage is unavailable or old metadata cannot safely be reused', async () => {
  panel();
  const failure = vi
    .spyOn(Storage.prototype, 'setItem')
    .mockImplementation(() => {
      throw Error('blocked');
    });
  try {
    await userEvent
      .setup()
      .click(screen.getByRole('button', { name: 'Send free enquiry' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'safe retry key',
    );
    expect(submitTravelRequest).not.toHaveBeenCalled();
  } finally {
    failure.mockRestore();
  }
});
it('handles success direct access honestly and restores only a validated tab receipt', () => {
  const app = render(
    <MemoryRouter initialEntries={['/plan-your-trip/success']}>
      <App />
    </MemoryRouter>,
  );
  expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
    'No confirmed enquiry',
  );
  app.unmount();
  sessionStorage.setItem(
    receiptKey,
    JSON.stringify({ receipt, confirmedAt: Date.now() }),
  );
  render(
    <MemoryRouter initialEntries={['/plan-your-trip/success']}>
      <App />
    </MemoryRouter>,
  );
  expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
    'We have received',
  );
  expect(storedReceipt()).toEqual(receipt);
});
it('renders Swedish submission confirmation and no booking promise', async () => {
  localStorage.setItem(languageStorageKey, 'sv');
  vi.mocked(submitTravelRequest).mockResolvedValueOnce(receipt);
  panel();
  await userEvent
    .setup()
    .click(
      screen.getByRole('button', { name: 'Skicka kostnadsfri förfrågan' }),
    );
  expect(await screen.findByRole('heading', { level: 1 })).toHaveTextContent(
    'Vi har tagit emot',
  );
  expect(
    screen.getByText(/inte att någon resa har bokats/),
  ).toBeInTheDocument();
});
it('keeps idempotency metadata free of personal data and rejects unsupported/expired metadata', () => {
  const first = submissionAttempt();
  expect(submissionAttempt()).toBe(first);
  expect(localStorage.getItem(attemptKey)).not.toContain('anna');
  localStorage.setItem(
    attemptKey,
    JSON.stringify({ key: first, createdAt: Date.now() - 8 * 86400000 }),
  );
  expect(() => submissionAttempt()).toThrow();
  expect(retryDeadline('1')).toBeGreaterThan(Date.now());
  expect(
    retryDeadline(new Date(Date.now() + 60000).toUTCString()),
  ).toBeGreaterThan(Date.now());
  expect(validReceipt({ ...receipt, status: 'booked' })).toBe(false);
});
it('maps server errors back into the complete wizard and allows editing after submission failure', async () => {
  vi.mocked(submitTravelRequest).mockRejectedValueOnce(
    new ApiError('safe', 400, { errors: { email: ['email'] } }),
  );
  render(
    <MemoryRouter initialEntries={['/plan-your-trip?service=flights']}>
      <App />
    </MemoryRouter>,
  );
  const next = async () =>
    userEvent.setup().click(screen.getByRole('button', { name: 'Continue' }));
  const change = (id: string, value: string) =>
    fireEvent.change(document.getElementById(id)!, { target: { value } });
  await next();
  change('origin', 'Stockholm');
  change('place', 'Lisbon');
  await next();
  change('departure', validWizard().dates.departure);
  change('return', validWizard().dates.return);
  await next();
  await next();
  change('amount', '25000');
  await next();
  await next();
  await next();
  change('name', 'Anna');
  change('email', 'anna@example.test');
  await next();
  await userEvent
    .setup()
    .click(screen.getByRole('button', { name: 'Send free enquiry' }));
  await waitFor(() =>
    expect(
      screen.getByRole('heading', { level: 2, name: 'Contact details' }),
    ).toBeInTheDocument(),
  );
  expect(document.getElementById('email')).toHaveValue('anna@example.test');
  expect(document.getElementById('email')).toHaveAttribute(
    'aria-invalid',
    'true',
  );
  expect(screen.getByRole('button', { name: 'Back to review' })).toBeEnabled();
});

it('does not treat an invalid receipt or a non-created response as persistence confirmation', async () => {
  const actual = await vi.importActual<
    typeof import('../../services/travelRequests')
  >('../../services/travelRequests');
  const response = {
    status: 201,
    statusText: 'Created',
    headers: {},
    config: { headers: new AxiosHeaders() },
    data: { ...receipt, status: 'booked' },
  };
  const post = vi.spyOn(apiClient, 'post').mockResolvedValue(response);
  try {
    await expect(
      actual.submitTravelRequest(payload, crypto.randomUUID()),
    ).rejects.toThrow('could not be confirmed');
    post.mockResolvedValue({ ...response, status: 200, data: receipt });
    await expect(
      actual.submitTravelRequest(payload, crypto.randomUUID()),
    ).rejects.toThrow('could not be confirmed');
  } finally {
    post.mockRestore();
  }
});
