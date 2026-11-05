export type ConversationStatus =
  'Open' | 'AwaitingCustomer' | 'AwaitingAgent' | 'Closed';
export type Priority = 'Normal' | 'High' | 'Urgent';
export interface ConversationItem {
  id: string;
  subject: string;
  status: ConversationStatus;
  priority?: Priority;
  requestId: string | null;
  requestReference: string | null;
  bookingId: string | null;
  bookingReference: string | null;
  customerName?: string;
  customerEmail?: string;
  preview: string;
  lastMessageAt: string;
  unreadCount: number;
}
export interface Message {
  id: string;
  sequence: number;
  senderRole: 'Customer' | 'Administrator';
  senderName: string;
  body: string;
  createdAt: string;
}
export interface ConversationEvent {
  id: string;
  type: string;
  previous: string | null;
  current: string | null;
  messageSequence: number | null;
  createdAt: string;
}
export interface ConversationDetail {
  conversation: ConversationItem;
  versionToken?: string;
  maxMessageLength: number;
  lastSequence: number;
  moreBefore: boolean;
  messages: Message[];
  emailNotifications: boolean;
  history: ConversationEvent[] | null;
}
export interface MessageSummary {
  unreadCount: number;
  awaitingReply: number;
  highPriority: number;
  recent: ConversationItem[];
}
export interface ConversationLink {
  id: string;
  kind: 'requests' | 'bookings';
  reference: string;
}
export interface PageResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
export interface ConversationFilters {
  page: number;
  status: string;
  priority: string;
  search: string;
  unread: boolean;
  requestId: string;
  bookingId: string;
}
