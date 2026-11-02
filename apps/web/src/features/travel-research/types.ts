import type { PageResult } from '../admin/model';
export type ResearchKind = 'Flight' | 'Accommodation' | 'Extra';
export type ProposalStatus =
  | 'Draft'
  | 'Sent'
  | 'Approved'
  | 'Rejected'
  | 'Expired'
  | 'Superseded'
  | 'Cancelled';
export interface FlightLeg {
  journey: number;
  airline: string;
  flightNumber: string;
  departureAirport: string;
  arrivalAirport: string;
  departureAt: string;
  arrivalAt: string;
  departureTimeZone: string;
  arrivalTimeZone: string;
}
export interface FlightDetails {
  legs: FlightLeg[];
  cabinClass: string;
  baggageDetails: string;
}
export interface AccommodationDetails {
  destination: string;
  address: string;
  accommodationType: string;
  roomType: string;
  checkIn: string;
  checkOut: string;
  guests: number;
  mealPlan: string;
  amenities: string;
}
export interface ExtraDetails {
  category: string;
  location: string;
  date: string | null;
}
export interface ResearchInput {
  kind: ResearchKind;
  title: string;
  description: string;
  priceMinorUnits: number;
  currency: string;
  priceBasis: string;
  quantity: number;
  supplierName: string;
  supplierUrl: string;
  priceCheckedAt: string;
  availabilityCheckedAt: string | null;
  conditions: string;
  notes: string;
  flight: FlightDetails | null;
  accommodation: AccommodationDetails | null;
  extra: ExtraDetails | null;
}
export interface ResearchSnapshot extends Omit<ResearchInput, 'notes'> {
  totalMinorUnits: number;
}
export interface ResearchOption {
  id: string;
  versionToken: string;
  option: ResearchSnapshot;
  notes: string;
  updatedAt: string;
}
export interface Workspace {
  eligible: boolean;
  status: 'NotStarted' | 'InProgress';
  versionToken: string | null;
  notes: string;
  options: ResearchOption[];
  proposals: PageResult<ProposalItem>;
}
export interface TripSnapshot {
  origin: string | null;
  destination: string | null;
  flexibleDestination: boolean;
  additionalStops: string[];
  dateMode: string;
  departure: string | null;
  return: string | null;
  earliest: string | null;
  latest: string | null;
  durationDays: number | null;
  adults: number;
  children: number;
  rooms: number | null;
}
export interface EstimatedCost {
  currency: string;
  flightsMinorUnits: number;
  accommodationMinorUnits: number;
  extrasMinorUnits: number;
  totalMinorUnits: number;
}
export interface Alternative {
  id: string;
  title: string;
  description: string;
  recommended: boolean;
  items: ResearchSnapshot[];
  estimatedCosts: EstimatedCost[];
}
export interface CustomerProposal {
  number: string;
  version: number;
  status: ProposalStatus;
  language: 'sv' | 'en';
  title: string;
  introduction: string;
  summary: string;
  termsSnapshot: string;
  termsVersion: string;
  acceptanceText: string;
  trip: TripSnapshot;
  paidServiceFee: {
    invoiceNumber: string;
    currency: string;
    amountMinorUnits: number;
    paidAt: string;
  };
  validUntil: string;
  approvedAt: string | null;
  rejectedAt: string | null;
  selectedAlternativeId: string | null;
  canRespond: boolean;
  alternatives: Alternative[];
}
export interface AdminProposal {
  customerLinkActive?: boolean;
  customerFeedback?: string | null;
  id: string;
  requestId: string;
  versionToken: string;
  customerEmail: string;
  isCurrentVersion: boolean;
  canRevise: boolean;
  proposal: CustomerProposal;
  itemIds: string[][];
}
export interface ProposalItem {
  selectedAlternativeTitle?: string | null;
  id: string;
  number: string;
  version: number;
  requestId: string;
  title: string;
  status: ProposalStatus;
  validUntil: string;
  updatedAt: string;
  selectedAlternativeId: string | null;
}
export interface AlternativeInput {
  title: string;
  description: string;
  recommended: boolean;
  itemIds: string[];
}
export interface ProposalInput {
  versionToken: string;
  title: string;
  introduction: string;
  summary: string;
  termsSnapshot: string;
  validUntil: string;
  alternatives: AlternativeInput[];
}
export interface ProposalConfiguration {
  blockingReasons: string[];
  termsSv: string;
  termsEn: string;
}
