import type { Translate } from './types';

const svPublic = {
  eyebrow: 'Personlig resehjälp',
  contactAction: 'Prata om din resa',
  pricingAction: 'Så fungerar priserna',
  faqAction: 'Läs vanliga frågor',
  services: {
    title: 'Rätt hjälp. För din resa.',
    description:
      'Välj den hjälp du behöver – från en enskild flygresa till en genomtänkt semester. Vi kommer överens om omfattningen innan arbetet börjar.',
    includes: 'Det här kan ingå',
    suitable: 'Passar dig som',
    extrasTitle: 'Lite mer än resan dit',
    extras:
      'Aktiviteter, upplevelser och praktiska detaljer kan ingå i en överenskommen reseplan. Säg till vad du vill ha hjälp med så tar vi med det i offerten.',
    boundary:
      'Inga bokningar genomförs utan ditt godkännande. Tillgänglighet, slutpriser och leverantörsvillkor bekräftas separat.',
    details: [
      {
        includes: [
          'Jämförelse av rutter, tider och mellanlandningar',
          'Genomgång av bagage och biljettvillkor',
          'Bokningshjälp enligt överenskommelse',
        ],
        suitable:
          'vill hitta lämpliga flygalternativ utan att jämföra allt själv.',
      },
      {
        includes: [
          'Urval av hotell eller andra boenden',
          'Jämförelse av läge, rum och bekvämligheter',
          'Genomgång av boendets villkor',
        ],
        suitable: 'vill hitta ett boende som passar resans tempo och behov.',
      },
      {
        includes: [
          'Samordning av flygtider och boendedatum',
          'Jämförelse av lämpliga kombinationer',
          'Tydlig översikt över separata resekostnader',
        ],
        suitable: 'vill ha hjälp med både resan och boendet.',
      },
      {
        includes: [
          'Planering av resrutt och vistelser',
          'Förslag på flyg, boende och upplevelser',
          'En sammanhängande plan utifrån dina önskemål',
        ],
        suitable:
          'vill ha hjälp att sätta ihop helheten, med utrymme för egna val.',
      },
    ],
  },
  pricing: {
    title: 'Tydligt från första steget.',
    description:
      'Din resa är personlig. Det är också offerten. Du får veta vad hjälpen kostar och vad som ingår innan du bestämmer dig.',
    fee: 'Serviceavgift',
    feeText:
      'Ersättningen för research, jämförelser och planering. Avgiften bestäms utifrån uppdragets omfattning och bekräftas i din personliga offert.',
    travel: 'Resekostnader',
    travelText:
      'Flyg, boende, aktiviteter och andra leverantörskostnader tillkommer separat. Du godkänner aktuella priser och villkor innan bokning.',
    quote: 'Personlig offert',
    quoteText:
      'Din personliga offert bekräftar omfattning, serviceavgift och villkor innan du bestämmer dig. Publicerade avgifter är vägledande; leverantörskostnader tillkommer separat.',
    columns: ['Tjänst', 'Omfattning', 'Serviceavgift'],
    scrollHint: 'Svep i tabellen för att jämföra alla kolumner.',
    scopes: [
      'Flygalternativ och biljettvillkor',
      'Boendealternativ och läge',
      'Samordning av resa och vistelse',
      'Resrutt, boende och upplevelser',
    ],
    stepsTitle: 'Du bestämmer innan vi börjar',
    steps: [
      'En första förfrågan blir kostnadsfri och innebär ingen bokning.',
      'Du får en offert med omfattning, serviceavgift och överenskommen tidsplan.',
      'Research börjar efter godkänd offert och betald serviceavgift.',
    ],
    note: 'Vi lovar inte marknadens lägsta pris. Målet är lämpliga alternativ, sparad tid och tydliga beslut. Avbokning och återbetalning beror på överenskommet uppdrag och leverantörernas villkor.',
  },
  about: {
    title: 'En människa bakom resplanen.',
    description:
      'Personlig resehjälp börjar med att lyssna. På vad du längtar efter, vad som är viktigt för dig och hur du vill använda din resebudget.',
    storyTitle: 'Planering med omtanke',
    story:
      'Med erfarenhet från resebranschen vill jag göra reseplaneringen mer överskådlig. Jag hjälper dig att jämföra alternativ och förstå skillnaderna, så att du kan fatta beslut som passar dig.',
    approachTitle: 'Dina önskemål sätter riktningen',
    approach:
      'En weekend för två, en familjesemester eller flera stopp på samma resa kräver olika lösningar. Vi kommer överens om vilken hjälp du behöver, och du behåller kontrollen över besluten.',
    identity:
      'Namn, personligt porträtt och mer information om verksamheten publiceras inför lansering. Inga specifika certifieringar, partnerskap eller antal år i branschen anges innan de har bekräftats.',
    values: [
      'Lyhördhet framför standardpaket',
      'Tydliga förväntningar och kostnader',
      'Ditt godkännande före bokning',
    ],
  },
  contact: {
    title: 'Låt oss prata om din nästa resa.',
    description:
      'Har du en allmän fråga om resehjälp eller hur tjänsten fungerar? Här hittar du kontaktvägar när verksamheten öppnar.',
    infoTitle: 'Personlig kontakt',
    unavailable:
      'Kontaktuppgifter är ännu inte publicerade. Formuläret är därför stängt och inga meddelanden skickas eller sparas.',
    emailLabel: 'E-post',
    formTitle: 'Skriv ett meddelande',
    name: 'Namn',
    email: 'Din e-postadress',
    subject: 'Ämne',
    message: 'Meddelande',
    privacy:
      'Skriv bara det som behövs för en allmän fråga. Skicka inte passnummer, betaluppgifter eller känsliga personuppgifter.',
    disclosure:
      'Formuläret förbereder ett utkast i ditt e-postprogram. Det skickas inte härifrån. Du granskar och skickar själv i e-postprogrammet.',
    prepare: 'Förbered e-postutkast',
    submit: 'Öppna e-postutkast',
    status:
      'Ditt utkast är förberett. Använd länken nedan för att öppna ditt e-postprogram. Inget meddelande har skickats från webbplatsen.',
    error:
      'Kontrollera fälten: ange namn, giltig e-postadress, ämne och ett meddelande. Använd inte enbart blanksteg.',
    availability:
      'Ingen omedelbar svarstid garanteras. Inga akuta reseärenden hanteras här.',
    wizard:
      'I reseplaneraren kan du förbereda, granska och skicka en kostnadsfri reseförfrågan.',
  },
  faq: {
    title: 'Bra att veta innan du reser.',
    description:
      'Svar på vanliga frågor om personlig resehjälp, serviceavgifter och hur planeringen går till.',
    items: [
      {
        question: 'Vad kan jag få hjälp med?',
        answer:
          'Du kan få hjälp med flyg, boende, en kombination av båda eller en mer komplett reseplan. Aktiviteter kan ingå efter överenskommelse.',
      },
      {
        question: 'Kostar den första förfrågan något?',
        answer:
          'Den första förfrågan blir kostnadsfri. Du får en personlig offert och bestämmer om du vill gå vidare. Du kan skicka en kostnadsfri förfrågan i reseplaneraren.',
      },
      {
        question: 'Vad är skillnaden mellan serviceavgift och resekostnad?',
        answer:
          'Serviceavgiften ersätter arbetet med att undersöka och planera resan. Flyg, boende och andra resekostnader är separata och godkänns innan bokning.',
      },
      {
        question: 'När börjar arbetet med min resa?',
        answer:
          'Research påbörjas när du har godkänt offerten och betalningen av den överenskomna serviceavgiften har verifierats. Följ instruktionerna i din faktura.',
      },
      {
        question: 'Bokar ni något utan mitt godkännande?',
        answer:
          'Nej. Du granskar reseförslaget, aktuella priser och villkor innan överenskomna bokningar genomförs. En förfrågan eller offert är ingen bekräftad bokning.',
      },
      {
        question: 'Garanterar ni det billigaste priset?',
        answer:
          'Nej. Målet är att hitta lämpliga alternativ utifrån dina önskemål och din budget, spara tid och hjälpa dig förstå dina val.',
      },
      {
        question: 'Vad händer om jag behöver avboka eller ändra?',
        answer:
          'Möjligheter och kostnader för ändring, avbokning och återbetalning beror på leverantörens villkor och vårt överenskomna uppdrag. Villkoren ska framgå innan du godkänner bokningen.',
      },
      {
        question: 'Kan jag boka eller betala på webbplatsen nu?',
        answer:
          'Du kan skicka en reseförfrågan och använda ett verifierat kundkonto för att följa dina ärenden, offerter och fakturor. Följ betalningsinstruktionerna i din faktura. En förfrågan är ingen bokning och inga leverantörsbokningar sker automatiskt. Det allmänna kontaktformuläret förbereder bara ett e-postutkast.',
      },
      {
        question: 'Kan jag få hjälp på engelska?',
        answer:
          'Ja, informationen på webbplatsen finns på svenska och engelska. Välj språk i sidhuvudet. Ditt val sparas i webbläsaren om lokal lagring är tillgänglig.',
      },
    ],
  },
} as const;

const enPublic = {
  eyebrow: 'Personal travel planning',
  contactAction: 'Talk about your journey',
  pricingAction: 'How pricing works',
  faqAction: 'Read frequently asked questions',
  services: {
    title: 'The right help. For your journey.',
    description:
      'Choose the help you need, from a single flight to a carefully considered holiday. We agree on the scope before work begins.',
    includes: 'What can be included',
    suitable: 'For travellers who',
    extrasTitle: 'Beyond getting there',
    extras:
      'Activities, experiences and practical details can be included in an agreed travel plan. Tell me what you need help with so it can be covered in your quote.',
    boundary:
      'No bookings are made without your approval. Availability, final prices and supplier conditions are confirmed separately.',
    details: [
      {
        includes: [
          'Comparison of routes, times and connections',
          'Review of baggage and ticket conditions',
          'Booking assistance as agreed',
        ],
        suitable:
          'want suitable flight options without doing all the comparisons themselves.',
      },
      {
        includes: [
          'Selection of hotels or other accommodation',
          'Comparison of location, rooms and amenities',
          'Review of accommodation conditions',
        ],
        suitable:
          'want a place to stay that fits the pace and needs of their trip.',
      },
      {
        includes: [
          'Coordination of flights and accommodation dates',
          'Comparison of suitable combinations',
          'Clear overview of separate travel costs',
        ],
        suitable: 'want help with both the journey and the place to stay.',
      },
      {
        includes: [
          'Planning routes and stays',
          'Suggestions for flights, accommodation and experiences',
          'A connected itinerary based on your preferences',
        ],
        suitable:
          'want help bringing the whole trip together, with room for their own choices.',
      },
    ],
  },
  pricing: {
    title: 'Clarity from the first step.',
    description:
      'Your journey is personal. So is your quote. You will know what the help costs and what is included before you decide.',
    fee: 'Service fee',
    feeText:
      'Payment for research, comparisons and planning. The fee depends on the scope of the work and is confirmed in your personal quote.',
    travel: 'Travel costs',
    travelText:
      'Flights, accommodation, activities and other supplier costs are separate. You approve current prices and conditions before booking.',
    quote: 'Personal quote',
    quoteText:
      'Your personal quotation confirms scope, service fee and conditions before you decide. Published fees are guidance; supplier travel costs are separate.',
    columns: ['Service', 'Scope', 'Service fee'],
    scrollHint: 'Swipe across the table to compare all columns.',
    scopes: [
      'Flight options and ticket conditions',
      'Accommodation options and location',
      'Coordinating your journey and stay',
      'Routes, accommodation and experiences',
    ],
    stepsTitle: 'You decide before we begin',
    steps: [
      'An initial enquiry will be free and does not make a booking.',
      'You receive a quote with scope, service fee and an agreed timeline.',
      'Research begins after you accept the quote and pay the service fee.',
    ],
    note: 'We do not promise the lowest price on the market. The aim is suitable options, saved time and informed decisions. Cancellation and refunds depend on the agreed work and supplier conditions.',
  },
  about: {
    title: 'A person behind the travel plan.',
    description:
      'Personal travel planning starts with listening. To what you look forward to, what matters to you and how you want to use your travel budget.',
    storyTitle: 'Planning with care',
    story:
      'With experience in the travel industry, I want to make planning easier to understand. I help you compare options and see the differences so you can make decisions that suit you.',
    approachTitle: 'Your wishes set the direction',
    approach:
      'A weekend for two, a family holiday or a journey with several stops calls for different solutions. We agree on the help you need, and you stay in control of the decisions.',
    identity:
      'A name, personal portrait and further business details will be published before launch. Specific certifications, partnerships and years of experience are not listed until confirmed.',
    values: [
      'Listening before standard packages',
      'Clear expectations and costs',
      'Your approval before booking',
    ],
  },
  contact: {
    title: 'Let’s talk about your next journey.',
    description:
      'Have a general question about travel planning or how the service works? Contact options will be available here when the business opens.',
    infoTitle: 'Personal contact',
    unavailable:
      'Contact details have not been published yet. The form is closed, and no messages are sent or stored.',
    emailLabel: 'Email',
    formTitle: 'Write a message',
    name: 'Name',
    email: 'Your email address',
    subject: 'Subject',
    message: 'Message',
    privacy:
      'Only include what is needed for a general question. Do not send passport numbers, payment details or sensitive personal information.',
    disclosure:
      'This form prepares a draft in your email application. It does not send from this website. You review and send it yourself in your email application.',
    prepare: 'Prepare email draft',
    submit: 'Open email draft',
    status:
      'Your draft is ready. Use the link below to open your email application. No message has been sent from this website.',
    error:
      'Check the fields: enter a name, valid email address, subject and message. Do not use only spaces.',
    availability:
      'An immediate response is not guaranteed. Urgent travel issues are not handled here.',
    wizard:
      'The travel wizard lets you prepare, review and send a free travel enquiry.',
  },
  faq: {
    title: 'Good to know before you go.',
    description:
      'Answers to common questions about personal travel planning, service fees and how the process works.',
    items: [
      {
        question: 'What can I get help with?',
        answer:
          'You can get help with flights, accommodation, a combination of both or a more complete itinerary. Activities can be included by agreement.',
      },
      {
        question: 'Does the initial enquiry cost anything?',
        answer:
          'The initial enquiry will be free. You receive a personal quote and decide whether to proceed. You can send a free enquiry in the travel wizard.',
      },
      {
        question: 'How is the service fee different from travel costs?',
        answer:
          'The service fee pays for the work of researching and planning. Flights, accommodation and other travel costs are separate and approved before booking.',
      },
      {
        question: 'When does work on my trip begin?',
        answer:
          'Research begins once you accept the quote and payment of the agreed service fee has been verified. Follow the instructions on your invoice.',
      },
      {
        question: 'Will you book anything without my approval?',
        answer:
          'No. You review the proposal, current prices and conditions before agreed bookings are made. An enquiry or quote is not a confirmed booking.',
      },
      {
        question: 'Do you guarantee the cheapest price?',
        answer:
          'No. The aim is to find suitable options for your wishes and budget, save you time and help you understand your choices.',
      },
      {
        question: 'What if I need to cancel or change my trip?',
        answer:
          'Options and costs for changes, cancellation and refunds depend on supplier conditions and our agreed work. Conditions must be clear before you approve a booking.',
      },
      {
        question: 'Can I book or pay on the website now?',
        answer:
          'You can send a travel enquiry and use a verified customer account to follow your requests, quotations and invoices. Follow the payment instructions on your invoice. An enquiry is not a booking and supplier bookings are never automatic. The general contact form only prepares an email draft.',
      },
      {
        question: 'Can I use the website in English?',
        answer:
          'Yes. Website information is available in Swedish and English. Choose a language in the header. Your choice is saved in your browser when local storage is available.',
      },
    ],
  },
} as const satisfies Translate<typeof svPublic>;

export const publicCopy = { sv: svPublic, en: enPublic } as const;
