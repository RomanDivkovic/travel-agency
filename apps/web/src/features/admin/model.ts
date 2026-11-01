import type { Service } from '../travel-request/model';
export const statuses = [
  'Submitted',
  'UnderReview',
  'AwaitingCustomer',
  'Closed',
] as const;
export type RequestStatus = (typeof statuses)[number];
export interface AdminUser {
  id: string;
  displayName: string;
  email: string;
  role: 'Administrator';
}
export interface RequestItem {
  id: string;
  referenceNumber: string;
  customerName: string;
  serviceType: Service;
  destination: string | null;
  flexibleDestination: boolean;
  dateMode: string;
  departure: string | null;
  return: string | null;
  earliest: string | null;
  latest: string | null;
  createdAt: string;
  updatedAt: string;
  status: RequestStatus;
}
export interface PageResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
export interface Dashboard {
  total: number;
  submitted: number;
  underReview: number;
  awaitingCustomer: number;
  closed: number;
  recentlyUpdated: RequestItem[];
}
export interface RequestDetail {
  id: string;
  referenceNumber: string;
  serviceType: Service;
  status: RequestStatus;
  language: 'sv' | 'en';
  contact: {
    name: string;
    email: string;
    phone: string | null;
    preferredMethod: 'email' | 'phone';
  };
  plan: {
    origin: string | null;
    destination: string | null;
    flexibleDestination: boolean;
    additionalStops: string[];
    dateMode: 'fixed' | 'flexible';
    departure: string | null;
    return: string | null;
    earliest: string | null;
    latest: string | null;
    durationDays: number | null;
    adults: number;
    childrenAges: number[];
    rooms: number | null;
    budgetAmount: number;
    currency: string;
    budgetScope: 'total' | 'per-person';
    preferencesJson: string;
    activities: boolean;
    specialRequests: string | null;
  };
  createdAt: string;
  updatedAt: string;
  version: string;
  allowedStatuses: RequestStatus[];
}
export interface Note {
  id: string;
  author: string;
  content: string;
  createdAt: string;
}
export interface Activity {
  id: string;
  type: 'Submitted' | 'StatusChanged' | 'NoteAdded';
  author: string | null;
  previousValue: RequestStatus | null;
  newValue: RequestStatus | null;
  createdAt: string;
}
export interface Filters {
  page: number;
  pageSize: number;
  status: string;
  serviceType: string;
  search: string;
  sort: string;
  from: string;
  to: string;
}
export const initialFilters: Filters = {
  page: 1,
  pageSize: 20,
  status: '',
  serviceType: '',
  search: '',
  sort: 'createdAt_desc',
  from: '',
  to: '',
};
