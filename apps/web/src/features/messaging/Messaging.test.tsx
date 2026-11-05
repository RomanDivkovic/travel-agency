import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router';
import { beforeEach, expect, it, vi } from 'vitest';
import { LanguageProvider } from '../../localization/LanguageProvider';
import { languageStorageKey } from '../../localization/useLanguage';
import { ApiError } from '../../services/apiClient';
import { MessageComposer } from './MessageComposer';
import { ConversationThread } from './ConversationThread';
import { ConversationControls } from './ConversationControls';
import { ConversationPage } from './ConversationPage';
import { InboxPage } from './InboxPage';
import { MessagingContext } from './context';
import { MessagesNavigation } from './MessagingIntegration';
import { messagingApi } from './api';
import type { ConversationDetail, MessageSummary } from './types';
vi.mock('./api', () => ({
  messagingApi: {
    list: vi.fn(),
    detail: vi.fn(),
    read: vi.fn(),
    change: vi.fn(),
    send: vi.fn(),
    preferences: vi.fn(),
  },
}));
const data: ConversationDetail = {
  conversation: {
    id: 'thread',
    subject: 'TEST booking question',
    status: 'AwaitingCustomer',
    requestId: 'request',
    requestReference: 'TRV-TEST',
    bookingId: null,
    bookingReference: null,
    preview: 'TEST body',
    lastMessageAt: '2026-11-05T18:00:00Z',
    unreadCount: 1,
  },
  maxMessageLength: 5000,
  lastSequence: 2,
  moreBefore: false,
  messages: [
    {
      id: 'one',
      sequence: 1,
      senderRole: 'Customer',
      senderName: 'TEST customer',
      body: 'TEST initial message',
      createdAt: '2026-11-05T17:00:00Z',
    },
    {
      id: 'two',
      sequence: 2,
      senderRole: 'Administrator',
      senderName: 'TEST agent',
      body: 'TEST literal <script>alert(1)</script>\nSecond line',
      createdAt: '2026-11-05T18:00:00Z',
    },
  ],
  emailNotifications: true,
  history: null,
};
const summary: MessageSummary = {
  unreadCount: 3,
  awaitingReply: 1,
  highPriority: 0,
  recent: [data.conversation],
};
function mount(node: React.ReactNode, route = '/account/messages') {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <LanguageProvider>{node}</LanguageProvider>
    </MemoryRouter>,
  );
}
beforeEach(() => {
  vi.resetAllMocks();
  localStorage.setItem(languageStorageKey, 'en');
  vi.mocked(messagingApi.read).mockResolvedValue(undefined);
  vi.mocked(messagingApi.detail).mockResolvedValue(data);
});
it('keeps a failed message and reuses its submission key until the content changes', async () => {
  const send = vi
    .fn()
    .mockRejectedValueOnce(new ApiError('offline', 0))
    .mockResolvedValueOnce(undefined);
  mount(<MessageComposer maxLength={100} onSend={send} />);
  const u = userEvent.setup();
  await u.type(screen.getByLabelText('Message', { exact: true }), 'TEST retry');
  await u.click(screen.getByRole('button', { name: 'Send message' }));
  expect(await screen.findByRole('alert')).toBeVisible();
  expect(screen.getByLabelText('Message', { exact: true })).toHaveValue(
    'TEST retry',
  );
  const firstKey = send.mock.calls[0]![1];
  await u.click(screen.getByRole('button', { name: 'Send message' }));
  await waitFor(() =>
    expect(screen.getByLabelText('Message', { exact: true })).toHaveValue(''),
  );
  expect(send.mock.calls[1]![1]).toBe(firstKey);
  expect(await screen.findByRole('status')).toHaveTextContent(
    'Message saved by the server.',
  );
});
it('prevents duplicate clicks and claims success only after server confirmation', async () => {
  let complete!: () => void;
  const send = vi.fn(
    () =>
      new Promise<void>((r) => {
        complete = r;
      }),
  );
  mount(<MessageComposer maxLength={100} onSend={send} />);
  const u = userEvent.setup();
  await u.type(
    screen.getByLabelText('Message', { exact: true }),
    'TEST pending',
  );
  await u.click(screen.getByRole('button', { name: 'Send message' }));
  expect(screen.getByRole('button', { name: 'Sending…' })).toBeDisabled();
  expect(screen.queryByRole('status')).not.toBeInTheDocument();
  await act(async () => complete());
  expect(await screen.findByRole('status')).toBeVisible();
  expect(send).toHaveBeenCalledOnce();
});
it('rejects whitespace and exposes an accessible message limit', async () => {
  mount(<MessageComposer maxLength={100} onSend={vi.fn()} />);
  expect(screen.getByRole('button', { name: 'Send message' })).toBeDisabled();
  await userEvent.type(
    screen.getByLabelText('Message', { exact: true }),
    '   ',
  );
  expect(screen.getByRole('button', { name: 'Send message' })).toBeDisabled();
  expect(screen.getByLabelText('Message', { exact: true })).toHaveAttribute(
    'maxLength',
    '100',
  );
  expect(screen.getByText('100 characters maximum')).toBeVisible();
});
it('renders plaintext safely and identifies both sender roles', () => {
  mount(<ConversationThread data={data} admin={false} />);
  expect(screen.getByText(/TEST literal <script>/)).toBeVisible();
  expect(document.querySelector('script')).toBeNull();
  expect(screen.getByText('Travel agent · TEST agent')).toBeVisible();
  expect(screen.getByText('Customer · TEST customer')).toBeVisible();
  expect(document.querySelector('time')).toHaveAttribute('datetime');
});
it('uses real backend unread counts for navigation', () => {
  mount(
    <MessagingContext value={{ summary, refresh: vi.fn() }}>
      <MessagesNavigation />
    </MessagingContext>,
  );
  expect(
    screen.getByRole('link', { name: 'Messages 3 unread messages' }),
  ).toHaveAttribute('href', '/account/messages');
});
it('does not fabricate a badge while summary data is unavailable', () => {
  mount(<MessagesNavigation />);
  expect(screen.queryByText(/unread messages/)).not.toBeInTheDocument();
});
it('shows empty and error inbox states and does not mark list entries read', async () => {
  vi.mocked(messagingApi.list).mockResolvedValue({
    items: [],
    page: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
  });
  mount(<InboxPage />);
  expect(
    await screen.findByText('No conversations match these filters.'),
  ).toBeVisible();
  expect(messagingApi.read).not.toHaveBeenCalled();
});
it('applies administrator status and unread filters through the API', async () => {
  vi.mocked(messagingApi.list).mockResolvedValue({
    items: [
      {
        ...data.conversation,
        priority: 'High',
        customerName: 'TEST customer',
        customerEmail: 'test@example.test',
      },
    ],
    page: 1,
    pageSize: 20,
    totalCount: 1,
    totalPages: 1,
  });
  mount(<InboxPage admin />, '/admin/messages');
  await screen.findByRole('link', { name: 'TEST booking question' });
  await userEvent.selectOptions(
    screen.getByLabelText('Conversation status'),
    'AwaitingAgent',
  );
  await userEvent.click(screen.getByLabelText('Unread only'));
  await userEvent.click(screen.getByRole('button', { name: 'Apply filters' }));
  await waitFor(() =>
    expect(messagingApi.list).toHaveBeenLastCalledWith(
      true,
      expect.objectContaining({ status: 'AwaitingAgent', unread: true }),
      expect.any(AbortSignal),
    ),
  );
  expect(messagingApi.read).not.toHaveBeenCalled();
});
it('marks only the displayed sequence read and keeps closed conversations read-only', async () => {
  vi.mocked(messagingApi.detail).mockResolvedValue({
    ...data,
    conversation: { ...data.conversation, status: 'Closed' },
  });
  mount(
    <Routes>
      <Route path="/account/messages/:id" element={<ConversationPage />} />
    </Routes>,
    '/account/messages/thread',
  );
  await screen.findByRole('heading', { name: 'TEST booking question' });
  await waitFor(() =>
    expect(messagingApi.read).toHaveBeenCalledWith(false, 'thread', 2),
  );
  expect(
    screen.queryByRole('button', { name: 'Send message' }),
  ).not.toBeInTheDocument();
  expect(
    screen.getByRole('link', { name: 'Start a conversation' }),
  ).toBeVisible();
});
it('shows localized Swedish composer labels', () => {
  localStorage.setItem(languageStorageKey, 'sv');
  mount(<MessageComposer maxLength={100} onSend={vi.fn()} />);
  expect(
    screen.getByRole('button', { name: 'Skicka meddelande' }),
  ).toBeVisible();
  expect(screen.getByLabelText('Meddelande', { exact: true })).toBeVisible();
});
it('preserves an unsent reply while a visible thread refreshes', async () => {
  mount(
    <Routes>
      <Route path="/account/messages/:id" element={<ConversationPage />} />
    </Routes>,
    '/account/messages/thread',
  );
  await screen.findByRole('heading', { name: 'TEST booking question' });
  const u = userEvent.setup();
  await u.type(
    screen.getByLabelText('Message', { exact: true }),
    'TEST unsent draft',
  );
  let resolve!: (v: ConversationDetail) => void;
  vi.mocked(messagingApi.detail).mockReturnValueOnce(
    new Promise((r) => {
      resolve = r;
    }),
  );
  await u.click(screen.getByRole('button', { name: 'Refresh messages' }));
  expect(screen.getByLabelText('Message', { exact: true })).toHaveValue(
    'TEST unsent draft',
  );
  await act(async () => resolve({ ...data, lastSequence: 3 }));
  expect(screen.getByLabelText('Message', { exact: true })).toHaveValue(
    'TEST unsent draft',
  );
});

it('saves an explicit administrator status change with the saved version token', async () => {
  vi.mocked(messagingApi.change).mockResolvedValue(undefined);
  const saved = vi.fn();
  mount(
    <ConversationControls
      data={{
        ...data,
        versionToken: 'saved-version',
        conversation: { ...data.conversation, priority: 'Normal' },
      }}
      onSaved={saved}
    />,
  );
  await userEvent.selectOptions(
    screen.getByLabelText('Conversation status', { exact: true }),
    'Closed',
  );
  await userEvent.click(
    screen.getAllByRole('button', { name: 'Save change' })[0]!,
  );
  await waitFor(() => expect(saved).toHaveBeenCalledOnce());
  expect(messagingApi.change).toHaveBeenCalledWith(
    'thread',
    'status',
    'Closed',
    'saved-version',
  );
});
it('does not claim a stale administrative change was saved', async () => {
  vi.mocked(messagingApi.change).mockRejectedValue(new ApiError('stale', 409));
  const saved = vi.fn();
  mount(
    <ConversationControls
      data={{
        ...data,
        versionToken: 'saved-version',
        conversation: { ...data.conversation, priority: 'Normal' },
      }}
      onSaved={saved}
    />,
  );
  await userEvent.selectOptions(
    screen.getByLabelText('Priority', { exact: true }),
    'High',
  );
  await userEvent.click(
    screen.getAllByRole('button', { name: 'Save change' })[1]!,
  );
  expect(await screen.findByRole('alert')).toHaveTextContent(
    'Refresh before retrying',
  );
  expect(saved).not.toHaveBeenCalled();
});
