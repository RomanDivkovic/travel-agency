import type {
  CustomerProposal,
  ResearchSnapshot,
  ResearchInput,
} from '../travel-research/types';
export const bookingStatuses = [
  'Pending',
  'InProgress',
  'PartiallyConfirmed',
  'Confirmed',
  'Failed',
  'Cancelled',
] as const;
export type BookingStatus = (typeof bookingStatuses)[number];
export const reservationStatuses = [
  'NotStarted',
  'InProgress',
  'AwaitingConfirmation',
  'Confirmed',
  'Failed',
  'Cancelled',
] as const;
export type ReservationStatus = (typeof reservationStatuses)[number];
export interface Evidence {
  customerName: string;
  channel: string;
  reference: string;
  approvedAt: string;
  confirmed: boolean;
}
export interface BookingItem {
  id: string;
  requestId: string;
  proposalId: string;
  reference: string;
  customerName: string;
  destination: string | null;
  departure: string | null;
  return: string | null;
  status: BookingStatus;
  confirmed: number;
  pending: number;
  failed: number;
  updatedAt: string;
}
export interface Reservation {
  id: string;
  proposalItemId: string;
  reservationType: string;
  status: ReservationStatus;
  approved: ResearchSnapshot;
  current: ResearchSnapshot;
  requiresReview: boolean;
  reapprovalRecorded: boolean;
  arrangementVersion: number;
  paymentResponsibility: string | null;
  paymentStatus: string;
  paymentArrangement: string;
  supplierReference: string;
  confirmationEvidence: string;
  documentReference: string;
  ticketingStatus: string;
  failureReason: string;
  cancellationReason: string;
  cancellationReference: string;
  cancellationChargeMinorUnits: number | null;
  confirmedAt: string | null;
  cancelledAt: string | null;
}
export interface Booking {
  booking: BookingItem;
  versionToken: string;
  revision: number;
  notes: string;
  eligible: boolean;
  proposal: CustomerProposal;
  reservations: Reservation[];
  notifications: {
    kind: string;
    attempts: number;
    sentAt: string | null;
    deadLetterAt: string | null;
  }[];
}
export interface BookingMetrics {
  inProgress: number;
  partiallyConfirmed: number;
  confirmed: number;
  failedReservations: number;
  upcomingDepartures: number;
}
export interface BookingHistory {
  id: string;
  reservationId: string | null;
  type: string;
  actor: string;
  createdAt: string;
  snapshotJson: string;
}
export interface ReservationInput {
  option: ResearchInput;
  paymentResponsibility: string;
  paymentStatus: string;
  paymentArrangement: string;
}
