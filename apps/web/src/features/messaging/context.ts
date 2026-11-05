import { createContext, useContext } from 'react';
import type { MessageSummary } from './types';
export const MessagingContext = createContext<{
  summary?: MessageSummary;
  error?: number;
  refresh: () => void;
}>({ refresh: () => {} });
export const useMessaging = () => useContext(MessagingContext);
