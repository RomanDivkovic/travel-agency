import { useLanguage } from '../../localization/useLanguage';
const sv = {
  acceptedLocked:
    'Godkända offerter är slutliga och kan inte revideras i detta arbetsflöde.',
  system: 'Systemet',
  title: 'Offerter',
  detail: 'Serviceoffert',
  customerTitle: 'Din personliga serviceoffert',
  create: 'Skapa offertutkast',
  edit: 'Redigera utkast',
  save: 'Spara utkast',
  saving: 'Sparar…',
  loading: 'Laddar…',
  empty: 'Inga offerter ännu.',
  back: 'Till offerterna',
  request: 'Till reseförfrågan',
  number: 'Offertnummer',
  version: 'Version',
  validUntil: 'Giltig till',
  created: 'Skapad',
  sentAt: 'Utfärdad',
  response: 'Kundens svar',
  customer: 'Kund',
  currency: 'Valuta',
  description: 'Beskrivning av tjänsten',
  included: 'Det här ingår',
  excluded: 'Det här ingår inte',
  notes: 'Meddelande till kunden',
  terms: 'Villkor',
  termsVersion: 'Villkorsversion',
  approvedTerms: 'Använd konfigurerade villkor',
  tax: 'Skattebehandling',
  taxRate: 'Skattesats (%)',
  items: 'Offertposter',
  item: 'Post',
  lineDescription: 'Beskrivning',
  quantity: 'Antal',
  unitPrice: 'Styckpris',
  addLine: 'Lägg till post',
  removeLine: 'Ta bort post',
  subtotal: 'Delsumma exklusive skatt',
  taxAmount: 'Skatt',
  total: 'Total serviceavgift',
  localPreview: 'Beräkningsförhandsvisning',
  preview: 'Visa sparad offert',
  hidePreview: 'Dölj förhandsvisning',
  previewHint:
    'Förhandsvisningen visar den senast sparade versionen. Spara ändringar innan du skickar.',
  draftHint:
    'Utkast – belopp och villkor är inte ett utfärdat erbjudande. Servern beräknar slutbeloppen.',
  unconfirmed:
    'Skattebehandlingen är inte bekräftad. Offerten kan inte utfärdas.',
  blocked:
    'Utfärdande är spärrat tills ägaren har konfigurerat godkänd skattebehandling, villkor, godkännandetext och webbplatsadress.',
  termsMismatch:
    'För att utfärda måste villkoren motsvara den konfigurerade, ägargodkända texten.',
  immutable:
    'Utfärdade offerter kan inte redigeras. En revidering skapar en ny version och gör den gamla länken obrukbar.',
  historical: 'Historisk version – kan inte ändras eller godkännas.',
  send: 'Utfärda och köa e-post',
  revise: 'Skapa ny version',
  cancel: 'Återkalla offerten',
  confirmSend: 'Utfärda den sparade offerten?',
  confirmRevise: 'Ersätt denna version med ett nytt utkast?',
  confirmCancel: 'Återkalla denna offert?',
  confirm: 'Ja, fortsätt',
  keep: 'Avbryt',
  queued:
    'Offerten är utfärdad och meddelandet är köat. E-postleverans behandlas separat.',
  saved: 'Utkastet har sparats.',
  cancelled: 'Offerten har återkallats.',
  history: 'Offerthistorik',
  noHistory: 'Ingen offerthistorik ännu.',
  search: 'Sök offert, förfrågan, namn eller e-post',
  status: 'Status',
  sort: 'Sortering',
  apply: 'Sök och filtrera',
  all: 'Alla',
  newest: 'Nyast först',
  oldest: 'Äldst först',
  updated: 'Senast uppdaterad',
  pageSize: 'Per sida',
  open: 'Öppna offert',
  error:
    'Det gick inte att bekräfta resultatet. Läs in den senaste versionen innan du försöker igen.',
  invalid:
    'Kontrollera belopp, poster, datum och textlängder. Använd högst två decimaler.',
  conflict:
    'Offerten har ändrats eller kan inte längre hanteras. Läs in senaste versionen.',
  unavailable:
    'Offertlänken är ogiltig, har löpt ut eller har återkallats. Använd den senaste länken från din reseagent.',
  retry: 'Läs in senaste versionen',
  rate: 'För många försök. Vänta en minut innan du försöker igen.',
  noBooking:
    'Ett godkännande avser den beskrivna resehjälpen. Det bokar inte flyg, boende eller andra researrangemang och startar inte arbetet automatiskt.',
  supplierCosts:
    'Serviceavgiften gäller reseagentens arbete. Kostnader hos flygbolag, boenden och andra leverantörer är separata.',
  accept: 'Godkänn serviceofferten',
  decline: 'Tacka nej',
  confirmDecline: 'Vill du tacka nej till denna version?',
  declineYes: 'Ja, tacka nej',
  acceptance: 'Ditt godkännande',
  explicit: 'Läs priset och villkoren ovan innan du bekräftar.',
  acceptedTitle: 'Tack! Du har godkänt offerten.',
  acceptedMessage:
    'Vi har registrerat ditt godkännande. Nästa steg är betalning av den överenskomna serviceavgiften. Du får mer information när betalningsfunktionen är tillgänglig.',
  declinedTitle: 'Du har tackat nej till offerten.',
  declinedMessage:
    'Vi har registrerat ditt svar. Tack för att du tog dig tid att gå igenom erbjudandet.',
  expired:
    'Giltighetstiden har gått ut. Denna version kan inte längre godkännas.',
  privateLink: 'Privat länk – dela den inte och undvik delade enheter.',
  home: 'Till webbplatsen',
  timeZone: 'Tidszon för datumfältet',
  limits:
    'Högst 20 poster, 1–100 per post. Belopp anges med punkt och högst två decimaler.',
  taxModes: {
    Unconfirmed: 'Inte bekräftad (endast utkast)',
    VatExclusive: 'Skatt tillkommer på styckpriset',
    VatIncluded: 'Skatt ingår i styckpriset',
    NoVat: 'Ingen skatt debiteras',
  },
  statuses: {
    Draft: 'Utkast',
    Sent: 'Utfärdad',
    Accepted: 'Godkänd',
    Declined: 'Avböjd',
    Expired: 'Utgången',
    Superseded: 'Ersatt',
    Cancelled: 'Återkallad',
  },
  events: {
    QuotationCreated: 'Offertutkast skapat.',
    QuotationEdited: 'Offertutkast uppdaterat.',
    QuotationSent: 'Offert utfärdad.',
    QuotationRevised: 'Ny offertversion skapad.',
    QuotationSuperseded: 'Offertversion ersatt.',
    QuotationCancelled: 'Offert återkallad.',
    QuotationAccepted: 'Kunden godkände offerten.',
    QuotationDeclined: 'Kunden tackade nej till offerten.',
    QuotationExpired: 'Offertens giltighetstid gick ut.',
  },
};
type Copy = {
  [K in keyof typeof sv]: (typeof sv)[K] extends string
    ? string
    : { [P in keyof (typeof sv)[K]]: string };
};
const en: Copy = {
  acceptedLocked:
    'Accepted quotations are final and cannot be revised in this workflow.',
  system: 'System',
  title: 'Quotations',
  detail: 'Service quotation',
  customerTitle: 'Your personal service quotation',
  create: 'Create quotation draft',
  edit: 'Edit draft',
  save: 'Save draft',
  saving: 'Saving…',
  loading: 'Loading…',
  empty: 'No quotations yet.',
  back: 'Back to quotations',
  request: 'Back to enquiry',
  number: 'Quotation number',
  version: 'Version',
  validUntil: 'Valid until',
  created: 'Created',
  sentAt: 'Issued',
  response: 'Customer response',
  customer: 'Customer',
  currency: 'Currency',
  description: 'Service description',
  included: 'Included services',
  excluded: 'Excluded services',
  notes: 'Customer-facing notes',
  terms: 'Terms',
  termsVersion: 'Terms version',
  approvedTerms: 'Use configured terms',
  tax: 'Tax treatment',
  taxRate: 'Tax rate (%)',
  items: 'Line items',
  item: 'Item',
  lineDescription: 'Description',
  quantity: 'Quantity',
  unitPrice: 'Unit price',
  addLine: 'Add item',
  removeLine: 'Remove item',
  subtotal: 'Subtotal excluding tax',
  taxAmount: 'Tax',
  total: 'Total service fee',
  localPreview: 'Calculation preview',
  preview: 'Preview saved quotation',
  hidePreview: 'Hide preview',
  previewHint:
    'The preview shows the last saved version. Save changes before issuing.',
  draftHint:
    'Draft – amounts and terms are not an issued offer. The server calculates authoritative totals.',
  unconfirmed: 'Tax treatment is unconfirmed. This quotation cannot be issued.',
  blocked:
    'Issuance is blocked until the owner configures approved tax treatment, terms, acceptance wording and website origin.',
  termsMismatch:
    'Issuing requires terms matching the configured, owner-approved text.',
  immutable:
    'Issued quotations cannot be edited. A revision creates a new version and invalidates the previous link.',
  historical: 'Historical version – cannot be changed or accepted.',
  send: 'Issue and queue email',
  revise: 'Create revision',
  cancel: 'Withdraw quotation',
  confirmSend: 'Issue the saved quotation?',
  confirmRevise: 'Replace this version with a new draft?',
  confirmCancel: 'Withdraw this quotation?',
  confirm: 'Yes, continue',
  keep: 'Cancel',
  queued:
    'The quotation is issued and the message is queued. Email delivery is processed separately.',
  saved: 'Draft saved.',
  cancelled: 'Quotation withdrawn.',
  history: 'Quotation history',
  noHistory: 'No quotation history yet.',
  search: 'Search quotation, enquiry, name or email',
  status: 'Status',
  sort: 'Sort order',
  apply: 'Search and filter',
  all: 'All',
  newest: 'Newest first',
  oldest: 'Oldest first',
  updated: 'Recently updated',
  pageSize: 'Per page',
  open: 'Open quotation',
  error:
    'The result could not be confirmed. Load the latest version before retrying.',
  invalid:
    'Check amounts, items, dates and text lengths. Use at most two decimal places.',
  conflict:
    'The quotation has changed or can no longer be acted on. Load the latest version.',
  unavailable:
    'This quotation link is invalid, expired or revoked. Use the latest link from your travel agent.',
  retry: 'Load latest version',
  rate: 'Too many attempts. Wait a minute before retrying.',
  noBooking:
    'Acceptance covers the described travel assistance. It does not book flights, accommodation or other travel arrangements, or automatically start work.',
  supplierCosts:
    'The service fee covers the travel agent’s work. Airline, accommodation and other supplier travel costs are separate.',
  accept: 'Accept service quotation',
  decline: 'Decline',
  confirmDecline: 'Decline this quotation version?',
  declineYes: 'Yes, decline',
  acceptance: 'Your acceptance',
  explicit: 'Review the fee and terms above before confirming.',
  acceptedTitle: 'Thank you! You have accepted the quotation.',
  acceptedMessage:
    'We have recorded your acceptance. The next step is payment of the agreed service fee. You will receive more information when payment functionality is available.',
  declinedTitle: 'You have declined the quotation.',
  declinedMessage:
    'We have recorded your response. Thank you for taking the time to review the offer.',
  expired:
    'The validity deadline has passed. This version can no longer be accepted.',
  privateLink: 'Private link – do not share it and avoid shared devices.',
  home: 'Visit website',
  timeZone: 'Date input time zone',
  limits:
    'Up to 20 items, quantity 1–100 each. Use a dot and at most two decimal places for amounts.',
  taxModes: {
    Unconfirmed: 'Unconfirmed (draft only)',
    VatExclusive: 'Tax added to unit price',
    VatIncluded: 'Tax included in unit price',
    NoVat: 'No tax charged',
  },
  statuses: {
    Draft: 'Draft',
    Sent: 'Issued',
    Accepted: 'Accepted',
    Declined: 'Declined',
    Expired: 'Expired',
    Superseded: 'Replaced',
    Cancelled: 'Withdrawn',
  },
  events: {
    QuotationCreated: 'Quotation draft created.',
    QuotationEdited: 'Quotation draft updated.',
    QuotationSent: 'Quotation issued.',
    QuotationRevised: 'Quotation revision created.',
    QuotationSuperseded: 'Quotation version replaced.',
    QuotationCancelled: 'Quotation withdrawn.',
    QuotationAccepted: 'Customer accepted the quotation.',
    QuotationDeclined: 'Customer declined the quotation.',
    QuotationExpired: 'Quotation validity ended.',
  },
};
export function useQuotationCopy() {
  return useLanguage().language === 'sv' ? sv : en;
}
export function quotationEventLabel(
  type: string,
  copy: ReturnType<typeof useQuotationCopy>,
) {
  return Object.hasOwn(copy.events, type)
    ? copy.events[type as keyof typeof copy.events]
    : type;
}
