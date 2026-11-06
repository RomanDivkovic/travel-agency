import { useCallback, type ReactNode } from 'react';
import { useLocation } from 'react-router';
import { messagingApi } from './api';
import { MessagingContext } from './context';
import { useMessagingResource } from './useMessagingResource';
export function MessagingProvider({
  admin,
  children,
}: {
  admin: boolean;
  children: ReactNode;
}) {
  const { pathname } = useLocation();
  const load = useCallback(
    (s: AbortSignal) => messagingApi.summary(admin, s),
    [admin],
  );
  const state = useMessagingResource(pathname, load);
  return (
    <MessagingContext
      value={{
        summary: state.data,
        error: state.error,
        refresh: state.refresh,
      }}
    >
      {children}
    </MessagingContext>
  );
}
