export type InvoiceStatus =
  'Draft' | 'Issued' | 'PaymentPending' | 'Paid' | 'Overdue' | 'Cancelled';
export type PaymentStatus = 'Pending' | 'Succeeded' | 'Failed' | 'Cancelled';
export interface InvoiceLine {
  description: string;
  quantity: number;
  unitPriceMinorUnits: number;
  taxRateBasisPoints: number;
  subtotalMinorUnits: number;
  taxMinorUnits: number;
  totalMinorUnits: number;
}
export interface Invoice {
  lastPaymentStatus?: PaymentStatus | null;
  number: string | null;
  quotationNumber: string;
  quotationVersion: number;
  status: InvoiceStatus;
  isSimulated: boolean;
  language: 'sv' | 'en';
  customerName: string;
  customerAddress: string;
  description: string;
  servicePeriod: string;
  currency: string;
  taxTreatment: string;
  subtotalMinorUnits: number;
  taxMinorUnits: number;
  totalMinorUnits: number;
  lines: InvoiceLine[];
  sellerName: string;
  sellerAddress: string;
  registrationNumber: string;
  taxInformation: string;
  terms: string;
  termsVersion: string;
  dueAt: string;
  issuedAt: string | null;
  paidAt: string | null;
}
export interface PaymentAttempt {
  id: string;
  provider: string;
  status: PaymentStatus;
  amountMinorUnits: number;
  currency: string;
  createdAt: string;
  confirmedAt: string | null;
  failureReasonCode: string | null;
  discrepancyCode: string | null;
  reconcileAttempts: number;
}
export interface AdminInvoice {
  customerLinkActive: boolean;
  id: string;
  requestId: string;
  quotationId: string;
  versionToken: string;
  invoice: Invoice;
  payments: PaymentAttempt[];
  researchEligible: boolean;
  sandboxResearchEligible: boolean;
}
export interface InvoiceItem {
  id: string;
  number: string | null;
  customerName: string;
  status: InvoiceStatus;
  currency: string;
  totalMinorUnits: number;
  dueAt: string;
  isSimulated: boolean;
}
export interface InvoicePage {
  items: InvoiceItem[];
  page: number;
  pageSize: number;
  total: number;
}
export interface PaymentSummary {
  currencies: {
    currency: string;
    isSimulated: boolean;
    paidMinorUnits: number;
    outstandingMinorUnits: number;
    pendingPayments: number;
    overdueInvoices: number;
  }[];
  recentPayments: InvoiceItem[];
}
export interface PaymentSession {
  sessionId: string;
  checkoutUrl: string;
  expiresAt: string;
}
export interface InvoiceConfiguration {
  provider: string;
  isSimulated: boolean;
  blockingReasons: string[];
}
export interface SandboxSession {
  resultUrl: string;
  amountMinorUnits: number;
  currency: string;
  status: PaymentStatus;
  expiresAt: string;
  isSimulated: true;
}
