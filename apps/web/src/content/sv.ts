export const sv = {
  meta: {
    homeTitle: 'Personlig reseplanering & bokningshjälp',
    description:
      'Få personlig hjälp att hitta, planera och boka flyg, boende och semesterresor. Skicka en kostnadsfri förfrågan och få ett erbjudande anpassat efter dina behov.',
  },
  navigation: {
    label: 'Huvudnavigation',
    mobileLabel: 'Mobilnavigation',
    open: 'Öppna menyn',
    close: 'Stäng menyn',
    skip: 'Hoppa till innehållet',
    items: [
      { label: 'Hem', destination: 'home' },
      { label: 'Tjänster', destination: 'services' },
      { label: 'Så fungerar det', destination: 'process' },
      { label: 'Om mig', destination: 'about' },
      { label: 'Kontakt', destination: 'contact' },
    ],
  },
  actions: {
    plan: 'Planera min resa',
    services: 'Se våra tjänster',
    learnMore: 'Läs mer',
    about: 'Läs mer om mig',
    home: 'Till startsidan',
  },
  hero: {
    title: ['Din nästa resa', 'börjar här.'],
    description:
      'Berätta vart du vill resa och vad du behöver hjälp med. Jag hjälper dig att hitta, planera och boka en resa som passar dina önskemål och din budget.',
    imageAlt: 'Illustrativ medelhavsby med terrakottatak vid en turkos vik.',
  },
  services: {
    title: 'Vad behöver du hjälp med?',
    description:
      'Oavsett om du bara behöver flygbiljetter eller vill ha hjälp med hela semestern finns personlig hjälp att få.',
    items: [
      {
        id: 'flyg',
        action: 'Få hjälp med flyg',
        icon: 'plane',
        title: 'Hitta & boka flyg',
        description:
          'Få hjälp att jämföra flygalternativ och hitta en resa som passar dina datum, önskemål och din budget.',
      },
      {
        id: 'boende',
        action: 'Få hjälp med boende',
        icon: 'bed',
        title: 'Hitta & boka boende',
        description:
          'Jag hjälper dig att hitta hotell eller andra boenden som passar din resa och dina behov.',
      },
      {
        id: 'flyg-boende',
        action: 'Planera flyg & boende',
        icon: 'luggage',
        title: 'Flyg & boende',
        description:
          'Få hjälp att planera och boka både resan dit och ett passande boende.',
      },
      {
        id: 'semester',
        action: 'Planera semestern',
        icon: 'compass',
        title: 'Komplett reseplanering',
        description:
          'Från första idé till färdig reseplan – få personlig hjälp med hela din semester.',
      },
    ],
  },
  process: {
    title: 'Så fungerar det',
    description: 'Från första tanke till färdig resplan.',
    items: [
      {
        title: 'Berätta om din resa',
        description:
          'Berätta vart du vill resa, när du vill åka och vad du behöver hjälp med. Din första förfrågan är kostnadsfri.',
      },
      {
        title: 'Få ett personligt erbjudande',
        description:
          'Jag går igenom din förfrågan och återkommer med ett personligt erbjudande.',
      },
      {
        title: 'Planeringen börjar',
        description:
          'När du har godkänt erbjudandet och betalat serviceavgiften börjar jag ta fram passande resealternativ.',
      },
      {
        title: 'Godkänn & boka',
        description:
          'Du får ta ställning till reseförslaget innan de överenskomna bokningarna genomförs.',
      },
    ],
    note: 'Serviceavgiften är separat från kostnaden för flyg och boende. Resekostnader och bokningsvillkor godkänns separat innan bokning.',
  },
  benefits: {
    title: ['Mer tid att längta.', 'Mindre tid att leta.'],
    items: [
      {
        icon: 'heart',
        title: 'Personlig service',
        description: 'Din resa planeras utifrån dina egna önskemål.',
      },
      {
        icon: 'clock',
        title: 'Spara tid',
        description: 'Slipp lägga timmar på att jämföra flyg och boenden.',
      },
      {
        icon: 'user',
        title: 'Erfarenhet från resebranschen',
        description: 'Få hjälp av någon med erfarenhet från resebranschen.',
      },
      {
        icon: 'wallet',
        title: 'Med din budget i åtanke',
        description: 'Få förslag som tar hänsyn till din budget.',
      },
    ],
  },
  about: {
    title: 'Personlig hjälp med din nästa resa',
    description:
      'Jag har erfarenhet från resebranschen och tycker om att hjälpa människor att hitta resor som passar just dem. Oavsett om du planerar en weekend, familjesemester eller längre resa hjälper jag dig gärna med att hitta rätt alternativ.',
    portrait: 'Porträtt kommer snart',
  },
  inspiration: {
    title: 'Vart går nästa resa?',
    description: 'Lite inspiration till din nästa ledighet.',
    items: [
      {
        image: 'city',
        title: 'En weekend i Europa',
        description:
          'Upptäck charmiga städer, god mat och nya perspektiv – perfekt för en långhelg.',
        alt: 'Illustrativ europeisk kanalstad med pastellfärgade fasader och en stenbro.',
      },
      {
        image: 'beach',
        title: 'Sol, hav & lugna dagar',
        description:
          'Njut av varma stränder, kristallklart vatten och tid att bara vara.',
        alt: 'Illustrativ lugn sandstrand med klart vatten, klippor och ett parasoll.',
      },
      {
        image: 'family',
        title: 'Tid tillsammans',
        description:
          'Resor som skapar minnen – upplevelser för hela familjen i en lugnare takt.',
        alt: 'Illustrativ familj som promenerar längs havet i en liten kuststad.',
      },
    ],
    note: 'AI-genererade inspirationsbilder. Inga bokningsbara erbjudanden eller aktuell tillgänglighet visas.',
  },
  finalCta: {
    title: 'Redo att planera din nästa resa?',
    description:
      'Skicka en kostnadsfri förfrågan och berätta vad du behöver hjälp med. Du får ett personligt erbjudande innan du bestämmer dig.',
  },
  footer: {
    description:
      'Personlig hjälp att hitta, planera och boka en resa som passar just dig.',
    explore: 'Utforska',
    services: 'Våra tjänster',
    contact: 'Kontakt',
    contactPlaceholder: 'Kontaktuppgifter publiceras inför lansering.',
    privacy: 'Integritetspolicy – kommer snart',
    terms: 'Villkor – kommer snart',
  },
  comingSoon: {
    title: 'Din reseförfrågan börjar snart här',
    description:
      'Formuläret för reseförfrågningar är inte öppet ännu. Här kommer du senare att kunna berätta om dina resplaner och få ett personligt erbjudande.',
    note: 'Inga förfrågningar eller bokningar tas emot på den här sidan ännu. En första förfrågan blir kostnadsfri. Researbete påbörjas först efter godkänt erbjudande och betald serviceavgift.',
    aboutTitle: 'Mer om din reseplanerare kommer snart',
    aboutDescription:
      'En personlig presentation, namn och porträtt publiceras inför lansering. Under tiden kan du läsa om hur personlig resehjälp fungerar på startsidan.',
  },
  notFound: {
    title: 'Sidan hittades inte',
    description:
      'Adressen verkar inte finnas. Du hittar reseinspiration och våra tjänster på startsidan.',
  },
  errors: {
    title: 'Något gick fel',
    description: 'Sidan kunde inte visas. Försök ladda om sidan.',
    reload: 'Ladda om',
  },
  diagnostics: {
    title: 'Resebyråns applikation körs',
    description: 'Diagnostik för anslutningen mellan frontend och API.',
    checking: 'Kontrollerar API…',
    connected: 'API anslutet',
    unavailable: 'API är inte tillgängligt',
  },
} as const;
