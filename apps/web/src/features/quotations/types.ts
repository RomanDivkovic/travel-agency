import type { PageResult, Activity } from '../admin/model';
import type { Service } from '../travel-request/model';
export const quotationStatuses = [
  'Draft',
  'Sent',
  'Accepted',
  'Declined',
  'Expired',
  'Superseded',
  'Cancelled',
] as const;
export type QuotationStatus = (typeof quotationStatuses)[number];
export const taxTreatments = [
  'Unconfirmed',
  'VatExclusive',
  'VatIncluded',
  'NoVat',
] as const;
export type TaxTreatment = (typeof taxTreatments)[number];
export interface QuotationLine {
  description: string;
  quantity: number;
  unitPriceMinorUnits: number;
  taxRateBasisPoints: number;
  subtotalMinorUnits: number;
  taxMinorUnits: number;
  totalMinorUnits: number;
}
export interface CustomerQuotation {
  number: string;
  version: number;
  status: QuotationStatus;
  language: 'sv' | 'en';
  customerName: string;
  description: string;
  includedServices: string;
  excludedServices: string;
  customerNotes: string;
  currency: 'SEK' | 'EUR' | 'USD';
  taxTreatment: TaxTreatment;
  subtotalMinorUnits: number;
  taxMinorUnits: number;
  totalMinorUnits: number;
  validUntil: string;
  termsSnapshot: string;
  termsVersion: string;
  acceptanceText: string;
  acceptedAt: string | null;
  declinedAt: string | null;
  lines: QuotationLine[];
}
export interface AdminQuotation {
  id: string;
  requestId: string;
  requestReference: string;
  versionToken: string;
  createdAt: string;
  updatedAt: string;
  sentAt: string | null;
  customerEmail: string;
  isCurrentVersion: boolean;
  canRevise: boolean;
  quotation: CustomerQuotation;
}
export interface QuotationListItem {
  id: string;
  requestId: string;
  requestReference: string;
  number: string;
  version: number;
  customerName: string;
  serviceType: Service;
  status: QuotationStatus;
  currency: CustomerQuotation['currency'];
  totalMinorUnits: number;
  createdAt: string;
  updatedAt: string;
  validUntil: string;
  sentAt: string | null;
  acceptedAt: string | null;
  declinedAt: string | null;
}
export interface QuotationConfiguration {
  issuanceReady: boolean;
  blockingReasons: string[];
  approvedTaxTreatments: TaxTreatment[];
  approvedTaxRatesBasisPoints: number[];
  termsVersion: string;
  termsSv: string;
  termsEn: string;
  defaultValidityDays: number;
}
export interface DraftInput {
  versionToken: string;
  description: string;
  includedServices: string;
  excludedServices: string;
  termsSnapshot: string;
  customerNotes: string;
  currency: CustomerQuotation['currency'];
  taxTreatment: TaxTreatment;
  validUntil: string;
  lines: Pick<
    QuotationLine,
    'description' | 'quantity' | 'unitPriceMinorUnits' | 'taxRateBasisPoints'
  >[];
}
export interface QuotationFilters {
  page: number;
  pageSize: number;
  status: string;
  search: string;
  sort: string;
}
export type QuotationPage = PageResult<QuotationListItem>;
export type QuotationHistory = PageResult<Activity>;
