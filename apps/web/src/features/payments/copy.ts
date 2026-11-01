import { useLanguage } from '../../localization/useLanguage';
const sv = {
  treatments: {
    VatExclusive: 'Priser exklusive moms',
    VatIncluded: 'Priser inklusive moms',
    NoVat: 'Ingen moms enligt godkänd behandling',
  },
  events: {
    InvoiceLinkRevoked: 'Fakturalänk återkallad',
    InvoiceCreated: 'Fakturautkast skapat',
    InvoiceEdited: 'Fakturauppgifter uppdaterade',
    InvoiceIssued: 'Faktura utfärdad',
    InvoiceCancelled: 'Fakturautkast avbrutet',
    InvoiceOverdue: 'Faktura förfallen',
    PaymentStarted: 'Betalningsförsök startat',
    ServiceFeeVerified: 'Servicebetalning verifierad',
    PaymentFailed: 'Betalningsförsök misslyckades',
    PaymentCancelled: 'Betalningsförsök avbröts',
  },
  revoke: 'Återkalla kundlänk',
  revoked: 'Kundlänken är återkallad eller har gått ut.',
  confirmRevoke:
    'Återkalla fakturalänken? Kunden kommer inte längre åt fakturan eller nya betalningssessioner via länken. Befintliga betalningsförsök och verifierade betalningar bevaras.',
  title: 'Fakturor & servicebetalningar',
  detail: 'Fakturadetaljer',
  customerTitle: 'Betala din serviceavgift',
  explanation:
    'Den här betalningen gäller den överenskomna avgiften för personlig resehjälp. Kostnader för flyg, boende och andra resetjänster ingår inte om det inte uttryckligen framgår av avtalet.',
  noBooking:
    'Serviceavgiften bekräftar inte flyg, hotell eller andra bokningar.',
  simulation:
    'TEST / SIMULERAD — inga pengar dras. Detta är inte en riktig faktura eller betalning.',
  loading: 'Hämtar…',
  busy: 'Arbetar…',
  error:
    'Kunde inte hämta eller uppdatera uppgifterna. Ingen betalning har bekräftats här. Försök igen eller kontakta reseagenten.',
  unavailable:
    'Fakturalänken är ogiltig, har återkallats eller har gått ut. Kontakta reseagenten.',
  conflict:
    'Uppgifterna har ändrats. Uppdatera sidan och kontrollera aktuell status innan du försöker igen.',
  blocked:
    'Utfärdande är spärrat tills företag, moms, villkor, redovisning och betalningslösning har godkänts.',
  retry: 'Hämta aktuell status',
  pay: 'Gå vidare till säker betalning',
  sandboxPay: 'Öppna simulerad testbetalning',
  download: 'Ladda ned fakturadokument',
  documentNote:
    'HTML-dokument som kan skrivas ut eller sparas som PDF i webbläsaren.',
  paidTitle: 'Tack! Din betalning är registrerad.',
  paidMessage:
    'Vi har bekräftat betalningen av din serviceavgift. Din reseförfrågan kan nu gå vidare till nästa steg i planeringen.',
  pendingTitle: 'Vi inväntar betalningsbekräftelse.',
  pendingMessage:
    'Bekräftelsen kan ta en stund. Vi kontrollerar betalningen på servern. Att återvända från betalningen innebär inte att den är bekräftad.',
  failedMessage:
    'Det senaste försöket bekräftades inte. Kontrollera aktuell status och försök igen via fakturan eller kontakta reseagenten.',
  failedTitle: 'Betalningen kunde inte genomföras.',
  result: 'Betalningsstatus',
  backInvoice: 'Till fakturan',
  privateLink:
    'Privat länk. Dela den bara med personer som ska ha tillgång till fakturan.',
  home: 'Till startsidan',
  number: 'Fakturanummer',
  quote: 'Serviceoffert',
  customer: 'Kund',
  address: 'Fakturaadress',
  period: 'Tjänsteperiod eller avtalad leverans',
  due: 'Förfallodatum',
  issued: 'Utfärdad',
  paidAt: 'Betalning verifierad',
  subtotal: 'Delsumma',
  tax: 'Moms',
  total: 'Totalt att betala',
  quantity: 'Antal',
  unit: 'Styckpris',
  rate: 'Momssats',
  terms: 'Fakturavillkor',
  taxTreatment: 'Momsbehandling',
  seller: 'Företagsuppgifter',
  registration: 'Registreringsnummer',
  draft: 'Utkast',
  create: 'Skapa fakturautkast',
  open: 'Öppna faktura',
  save: 'Spara fakturauppgifter',
  issue: 'Utfärda faktura',
  cancel: 'Avbryt utkast',
  confirmIssue: 'Utfärda och köa fakturamejl? Beloppen och uppgifterna låses.',
  confirmCancel:
    'Avbryta fakturautkastet? En ny primärfaktura skapas inte automatiskt.',
  confirm: 'Bekräfta',
  keep: 'Behåll',
  saved: 'Uppgifterna sparades.',
  queued:
    'Fakturan utfärdades och mejlet köades. Det innebär inte att mejlet har levererats.',
  immutable:
    'Beloppen kommer från den accepterade offerten och kan inte ändras. Utfärdade fakturor kräver ett godkänt rättelseförfarande.',
  attempts: 'Betalningsförsök',
  noAttempts: 'Inga betalningsförsök ännu.',
  eligible:
    'Verifierad servicebetalning — förfrågan är redo för framtida reseplanering.',
  sandboxEligible:
    'Endast testbehörighet registrerad. En simulerad betalning aktiverar inte verkligt researbete.',
  search: 'Sök fakturanummer',
  filter: 'Filtrera',
  all: 'Alla statusar',
  status: 'Status',
  empty: 'Inga fakturor hittades.',
  previous: 'Föregående',
  next: 'Nästa',
  page: 'Sida',
  overview: 'Betalningsöversikt',
  outstanding: 'Utestående',
  paid: 'Betalda serviceavgifter',
  pending: 'Väntande betalningar',
  overdue: 'Förfallna fakturor',
  recent: 'Senast betalda fakturor',
  noMetrics: 'Inga fakturor ännu.',
  sandboxTitle: 'Simulerad betalning — endast utveckling',
  sandboxInfo:
    'Detta är en lokal simulator, inte en betalningsleverantör. Ange inga kortuppgifter. Välj ett testresultat nedan.',
  simulateSuccess: 'Simulera verifierad betalning',
  simulateFailure: 'Simulera misslyckad betalning',
  simulateCancel: 'Simulera avbruten betalning',
  simulatedDone:
    'Testresultatet har registrerats. Öppna fakturalänken igen för serververifierad status.',
  reconciliation: 'Avvikelse — kräver granskning',
  statuses: {
    Draft: 'Utkast',
    Issued: 'Utfärdad',
    PaymentPending: 'Väntar på betalning',
    Paid: 'Betald',
    Overdue: 'Förfallen',
    Cancelled: 'Avbruten',
    Pending: 'Väntande',
    Succeeded: 'Verifierad',
    Failed: 'Misslyckad',
  },
};
const en: typeof sv = {
  treatments: {
    VatExclusive: 'Prices exclude VAT',
    VatIncluded: 'Prices include VAT',
    NoVat: 'No VAT under approved treatment',
  },
  events: {
    InvoiceLinkRevoked: 'Invoice link revoked',
    InvoiceCreated: 'Invoice draft created',
    InvoiceEdited: 'Invoice details updated',
    InvoiceIssued: 'Invoice issued',
    InvoiceCancelled: 'Invoice draft cancelled',
    InvoiceOverdue: 'Invoice overdue',
    PaymentStarted: 'Payment attempt started',
    ServiceFeeVerified: 'Service fee verified',
    PaymentFailed: 'Payment attempt failed',
    PaymentCancelled: 'Payment attempt cancelled',
  },
  revoke: 'Revoke customer link',
  revoked: 'The customer link is revoked or expired.',
  confirmRevoke:
    'Revoke this invoice link? The customer will lose invoice access and cannot start new payment sessions through it. Existing payment attempts and verified payments are preserved.',
  title: 'Invoices & service payments',
  detail: 'Invoice details',
  customerTitle: 'Pay your service fee',
  explanation:
    'This payment covers the agreed fee for personal travel assistance. Flights, accommodation and other travel services are excluded unless explicitly included in the agreement.',
  noBooking:
    'The service fee does not confirm flights, hotels or other bookings.',
  simulation:
    'TEST / SIMULATED — no money is collected. This is not a real invoice or payment.',
  loading: 'Loading…',
  busy: 'Working…',
  error:
    'Unable to load or update these details. No payment has been confirmed here. Try again or contact your travel agent.',
  unavailable:
    'This invoice link is invalid, revoked or expired. Contact your travel agent.',
  conflict:
    'The details changed. Refresh and check the current status before retrying.',
  blocked:
    'Issuance is blocked until business details, tax, terms, accounting and payment configuration are approved.',
  retry: 'Check current status',
  pay: 'Continue to secure payment',
  sandboxPay: 'Open simulated test payment',
  download: 'Download invoice document',
  documentNote:
    'HTML document that you can print or save as PDF in your browser.',
  paidTitle: 'Thank you! Your payment is registered.',
  paidMessage:
    'We have verified your service fee payment. Your travel request can now proceed to the next planning stage.',
  pendingTitle: 'We are awaiting payment confirmation.',
  pendingMessage:
    'Confirmation may take a little while. We check payment on the server. Returning from checkout does not confirm payment.',
  failedMessage:
    'The latest attempt was not confirmed. Check the current status and retry through the invoice, or contact your travel agent.',
  failedTitle: 'Payment could not be completed.',
  result: 'Payment status',
  backInvoice: 'Back to invoice',
  privateLink:
    'Private link. Only share it with people who should have access to this invoice.',
  home: 'Back to homepage',
  number: 'Invoice number',
  quote: 'Service quotation',
  customer: 'Customer',
  address: 'Billing address',
  period: 'Service period or agreed delivery',
  due: 'Due date',
  issued: 'Issued',
  paidAt: 'Payment verified',
  subtotal: 'Subtotal',
  tax: 'Tax',
  total: 'Total payable',
  quantity: 'Quantity',
  unit: 'Unit price',
  rate: 'Tax rate',
  terms: 'Invoice terms',
  taxTreatment: 'Tax treatment',
  seller: 'Business details',
  registration: 'Registration number',
  draft: 'Draft',
  create: 'Create invoice draft',
  open: 'Open invoice',
  save: 'Save invoice details',
  issue: 'Issue invoice',
  cancel: 'Cancel draft',
  confirmIssue:
    'Issue and queue the invoice email? Amounts and details will be frozen.',
  confirmCancel:
    'Cancel this draft? A new primary invoice will not be created automatically.',
  confirm: 'Confirm',
  keep: 'Keep',
  saved: 'Details saved.',
  queued:
    'Invoice issued and email queued. This does not mean the email was delivered.',
  immutable:
    'Amounts come from the accepted quotation and cannot be changed. Issued invoices require an approved correction workflow.',
  attempts: 'Payment attempts',
  noAttempts: 'No payment attempts yet.',
  eligible:
    'Service payment verified — this request is eligible for future travel planning.',
  sandboxEligible:
    'Test eligibility only. Simulated payments do not activate real travel work.',
  search: 'Search invoice number',
  filter: 'Filter',
  all: 'All statuses',
  status: 'Status',
  empty: 'No invoices found.',
  previous: 'Previous',
  next: 'Next',
  page: 'Page',
  overview: 'Payment overview',
  outstanding: 'Outstanding',
  paid: 'Paid service fees',
  pending: 'Pending payments',
  overdue: 'Overdue invoices',
  recent: 'Recently paid invoices',
  noMetrics: 'No invoices yet.',
  sandboxTitle: 'Simulated payment — development only',
  sandboxInfo:
    'This is a local simulator, not a payment provider. Do not enter card details. Choose a test outcome below.',
  simulateSuccess: 'Simulate verified payment',
  simulateFailure: 'Simulate failed payment',
  simulateCancel: 'Simulate cancelled payment',
  simulatedDone:
    'Test outcome recorded. Reopen your invoice link to view server-verified status.',
  reconciliation: 'Discrepancy — review required',
  statuses: {
    Draft: 'Draft',
    Issued: 'Issued',
    PaymentPending: 'Awaiting payment',
    Paid: 'Paid',
    Overdue: 'Overdue',
    Cancelled: 'Cancelled',
    Pending: 'Pending',
    Succeeded: 'Verified',
    Failed: 'Failed',
  },
};
export function usePaymentCopy() {
  return useLanguage().language === 'sv' ? sv : en;
}
