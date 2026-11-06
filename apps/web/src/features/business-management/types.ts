export type Language = 'sv' | 'en';
export interface BusinessData {
  businessName: string;
  legalBusinessName: string;
  organizationNumber: string;
  contactEmail: string;
  contactPhone: string;
  websiteUrl: string;
  address: string;
  postalCode: string;
  city: string;
  country: string;
  defaultLanguage: Language;
  defaultCurrency: string;
  timeZone: string;
  publicContactVisible: boolean;
  openingHours: { day: number; opens: string; closes: string }[];
  exceptions: {
    date: string;
    closed: boolean;
    opens: string;
    closes: string;
  }[];
}
export interface ServiceText {
  name: string;
  shortDescription: string;
  fullDescription: string;
  included: string;
  excluded: string;
  suitable: string;
  cta: string;
}
export interface ServiceData {
  code: string;
  enabled: boolean;
  displayOrder: number;
  sv: ServiceText;
  en: ServiceText;
  pricing: {
    type: 'FixedFee' | 'StartingFrom' | 'CustomQuotation';
    amountMinor: number | null;
    currency: string;
    effectiveFrom: string | null;
    effectiveUntil: string | null;
  };
}
export interface ContentImage {
  id: string;
  altSv: string;
  altEn: string;
  decorative: boolean;
}
export interface PageData {
  sv: Record<string, string>;
  en: Record<string, string>;
  image: ContentImage | null;
}
export interface FaqData {
  enabled: boolean;
  displayOrder: number;
  sv: { question: string; answer: string };
  en: { question: string; answer: string };
}
export interface PolicyData {
  sv: { title: string; content: string };
  en: { title: string; content: string };
  effectiveFrom: string;
  legallyReviewed: boolean;
  reviewedBy: string;
  reviewedOn: string | null;
}
export interface PreferencesData {
  agentMessageEmail: boolean;
  customerMessageEmail: boolean;
}
export type CmsData =
  | BusinessData
  | ServiceData
  | PageData
  | FaqData
  | PolicyData
  | PreferencesData;
export interface ContentDetail {
  key: string;
  versionToken: string;
  draft: CmsData;
  publishedVersion: number;
  updatedAt: string | null;
  updatedBy: string | null;
  versions: {
    version: number;
    data: CmsData;
    publishedAt: string;
    publishedBy: string | null;
  }[];
}
export interface ContentItem {
  titleSv?: string | null;
  titleEn?: string | null;
  key: string;
  publishedVersion: number;
  updatedAt: string | null;
}
export interface PublicContent {
  documents: Record<string, CmsData>;
}
export interface MediaItem extends ContentImage {
  fileName: string;
  contentType: string;
  archived: boolean;
  versionToken: string;
}
export const serviceCodes = [
  'FlightsOnly',
  'AccommodationOnly',
  'FlightsAndAccommodation',
  'CompleteTrip',
] as const;
export const cardIds = ['flyg', 'boende', 'flyg-boende', 'semester'];
export const wizardServices = [
  'flights',
  'accommodation',
  'flight-accommodation',
  'complete',
];
