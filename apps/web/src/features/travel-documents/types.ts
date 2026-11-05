import type { TripSnapshot } from '../travel-research/types';
export const documentTypes = [
  'FlightTicket',
  'BoardingPass',
  'FlightConfirmation',
  'HotelConfirmation',
  'TransferVoucher',
  'ActivityVoucher',
  'TravelInsurance',
  'GeneralTravelInformation',
  'Other',
] as const;
export type DocumentType = (typeof documentTypes)[number];
export interface DocumentVersion {
  id: string;
  version: number;
  name: string;
  description: string;
  originalFileName: string;
  contentType: string;
  size: number;
  status: 'Draft' | 'Published' | 'Archived' | 'Deleted';
  scan: 'Quarantined' | 'Clean' | 'Infected';
  createdAt: string;
  publishedAt: string | null;
}
export interface TravelDocument {
  id: string;
  bookingId: string;
  reservationId: string | null;
  reservationTitle: string | null;
  type: DocumentType;
  currentVersion: number;
  publishedVersion: number | null;
  versionToken?: string;
  versions: DocumentVersion[];
}
export interface DocumentLibrary {
  reference: string;
  items: TravelDocument[];
  page: number;
  totalPages: number;
  totalCount: number;
  maxBytes: number;
  reservations: { id: string; title: string; status: string }[];
}
export interface DocumentMetadata {
  name: string;
  description: string;
  type: DocumentType;
  reservationId: string | null;
  noIdentityDocuments: boolean;
}
export interface ItineraryEntry {
  id: string;
  kind: string;
  event: string;
  title: string;
  description: string;
  date: string | null;
  at: string | null;
  timeZone: string | null;
  location: string;
  supplierReference: string;
  confirmed: boolean;
  sortOrder: number;
}
export interface ItineraryVersion {
  id: string;
  version: number;
  status: 'Draft' | 'Published' | 'Superseded' | 'Archived';
  title: string;
  description: string;
  trip: TripSnapshot;
  entries: ItineraryEntry[];
  publishedAt: string | null;
  createdAt: string;
}
export interface Itinerary {
  bookingId: string;
  reference: string;
  versionToken?: string;
  currentVersion: number;
  publishedVersion: number | null;
  sourceChanged: boolean;
  versions: ItineraryVersion[];
}
export interface CustomEntry {
  id: string | null;
  title: string;
  description: string;
  date: string | null;
  localTime: string | null;
  timeZone: string | null;
  utcOffset: string | null;
  location: string;
  sortOrder: number;
}
export interface ItinerarySave {
  versionToken: string;
  title: string;
  description: string;
  customEntries: CustomEntry[];
}
