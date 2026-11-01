import { createContext, useContext } from 'react';
import type { AdminUser } from './model';
export const AuthContext = createContext<{
  user: AdminUser | null | undefined;
  failed: boolean;
  forbidden: boolean;
  refresh: () => void;
  setUser: (user: AdminUser | null) => void;
}>({
  user: undefined,
  failed: false,
  forbidden: false,
  refresh: () => {},
  setUser: () => {},
});
export function useAdminAuth() {
  return useContext(AuthContext);
}
export function safeAdminReturn(value: unknown) {
  return typeof value === 'string' &&
    /^\/admin(?:\/(?:requests|quotations)(?:\/[0-9a-f-]{36})?)?$/i.test(value)
    ? value
    : '/admin';
}
