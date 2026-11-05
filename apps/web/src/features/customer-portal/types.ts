import type { PageResult } from '../admin/model';
import type {
  CustomerProposal,
  ResearchSnapshot,
  TripSnapshot,
} from '../travel-research/types';
import type { CustomerQuotation } from '../quotations/types';
import type { Invoice } from '../payments/types';
export type ResourceKind =
  'requests' | 'quotations' | 'invoices' | 'proposals' | 'bookings';
export interface CustomerUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  preferredLanguage: 'sv' | 'en';
  lastLoginAt: string | null;
  sessionExpiresAt: string | null;
}
export interface Registration {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  passwordConfirmation: string;
  phone: string;
  preferredLanguage: 'sv' | 'en';
}
export interface Profile {
  firstName: string;
  lastName: string;
  phone: string;
  preferredLanguage: 'sv' | 'en';
}
export interface Journey {
  stage: string;
  completedStages: string[];
  nextAction: string | null;
}
export interface PortalItem {
  id: string;
  requestId: string;
  kind: ResourceKind;
  number: string;
  title: string;
  status: string;
  destination: string | null;
  departure: string | null;
  currency: string | null;
  amountMinorUnits: number | null;
  validUntil: string | null;
  updatedAt: string;
  journey: Journey | null;
  isSimulated: boolean;
}
export interface CustomerRequest {
  id: string;
  reference: string;
  service: string;
  status: string;
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
  journey: Journey;
}
export interface RequestDetail {
  request: CustomerRequest;
  related: PortalItem[];
}
export interface PortalActivity {
  id: string;
  requestId: string;
  type: string;
  createdAt: string;
}
export interface Dashboard {
  customer: CustomerUser;
  requests: CustomerRequest[];
  actionsRequired: PortalItem[];
  upcomingTrips: PortalItem[];
  recentActivity: PortalActivity[];
}
export interface CustomerBooking {
  id: string;
  requestId: string;
  reference: string;
  status: string;
  trip: TripSnapshot;
  reservations: {
    title: string;
    type: string;
    status: string;
    confirmedDetails: ResearchSnapshot | null;
    supplierReference: string | null;
    confirmedAt: string | null;
    ticketingStatus: string | null;
  }[];
}
export type { PageResult, CustomerProposal, CustomerQuotation, Invoice };
