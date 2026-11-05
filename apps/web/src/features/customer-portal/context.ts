import { createContext, useContext } from 'react';
import type { CustomerUser } from './types';
export const CustomerSessionContext = createContext<{
  user: CustomerUser | null | undefined;
  failed: boolean;
  setUser: (u: CustomerUser | null) => void;
  refresh: () => void;
}>({ user: undefined, failed: false, setUser: () => {}, refresh: () => {} });
export const useCustomerSession = () => useContext(CustomerSessionContext);
export function safeCustomerReturn(value: unknown) {
  return typeof value === 'string' &&
    /^\/account(?:\/(?:profile|security|(?:requests|quotations|invoices|proposals|bookings)(?:\/[0-9a-f-]{36}(?:\/(?:documents|itinerary))?)?))?$/.test(
      value,
    )
    ? value
    : '/account';
}
