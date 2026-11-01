import type { Translations } from './types';

export const en = {
  meta: {
    homeTitle: 'Personal travel planning & booking assistance',
    description:
      'Personal help with finding, planning and booking flights, accommodation and holidays. Explore our services and learn how tailored travel planning works.',
  },
  navigation: {
    label: 'Main navigation',
    mobileLabel: 'Mobile navigation',
    open: 'Open menu',
    close: 'Close menu',
    skip: 'Skip to content',
    items: [
      { label: 'Home', destination: 'home' },
      { label: 'Services', destination: 'services' },
      { label: 'Pricing', destination: 'pricing' },
      { label: 'About me', destination: 'about' },
      { label: 'Contact', destination: 'contact' },
      { label: 'FAQ', destination: 'faq' },
    ],
  },
  actions: {
    plan: 'Plan my trip',
    services: 'Explore our services',
    learnMore: 'Learn more',
    about: 'More about me',
    home: 'Back to home',
  },
  hero: {
    title: ['Your next journey', 'starts here.'],
    description:
      'Tell me where you want to go and what you need help with. I help you find, plan and book a trip that fits your wishes and your budget.',
    imageAlt:
      'Illustrative Mediterranean village with terracotta roofs beside a turquoise bay.',
  },
  services: {
    title: 'What would you like help with?',
    description:
      'From finding a flight to planning an entire holiday, there is personal help for your journey.',
    items: [
      {
        id: 'flyg',
        action: 'Get help with flights',
        icon: 'plane',
        title: 'Find & book flights',
        description:
          'Get help comparing flight options and finding a journey that suits your dates, preferences and budget.',
      },
      {
        id: 'boende',
        action: 'Get help with accommodation',
        icon: 'bed',
        title: 'Find & book accommodation',
        description:
          'I help you find hotels or other accommodation that suits your trip and your needs.',
      },
      {
        id: 'flyg-boende',
        action: 'Plan flights & accommodation',
        icon: 'luggage',
        title: 'Flights & accommodation',
        description:
          'Get help planning both your journey and a suitable place to stay.',
      },
      {
        id: 'semester',
        action: 'Plan your holiday',
        icon: 'compass',
        title: 'Complete travel planning',
        description:
          'From your first idea to a considered itinerary, get personal help planning your whole holiday.',
      },
    ],
  },
  process: {
    title: 'How it works',
    description: 'From the first idea to a considered travel plan.',
    items: [
      {
        title: 'Tell me about your trip',
        description:
          'Share where and when you want to travel and what you need help with. Your initial enquiry is free.',
      },
      {
        title: 'Receive a personal quote',
        description:
          'I review your enquiry and return with a quote tailored to your needs.',
      },
      {
        title: 'Planning begins',
        description:
          'Once you accept the quote and pay the service fee, I start researching suitable travel options.',
      },
      {
        title: 'Approve & book',
        description:
          'You review the proposal before any agreed bookings are made.',
      },
    ],
    note: 'The service fee is separate from flights and accommodation. Travel costs and booking conditions must be approved separately before booking.',
  },
  benefits: {
    title: ['More time to look forward.', 'Less time searching.'],
    items: [
      {
        icon: 'heart',
        title: 'Personal service',
        description: 'Your journey is planned around your own wishes.',
      },
      {
        icon: 'clock',
        title: 'Save time',
        description: 'Spend less time comparing flights and places to stay.',
      },
      {
        icon: 'user',
        title: 'Travel industry experience',
        description:
          'Get help from someone with experience in the travel industry.',
      },
      {
        icon: 'wallet',
        title: 'Your budget in mind',
        description: 'Receive suggestions that take your budget into account.',
      },
    ],
  },
  about: {
    title: 'Personal help with your next journey',
    description:
      'I have experience in the travel industry and enjoy helping people find journeys that suit them. Whether you are planning a weekend away, a family holiday or a longer adventure, I can help you explore the right options.',
    portrait: 'Portrait coming soon',
  },
  inspiration: {
    title: 'Where will you go next?',
    description: 'A little inspiration for your next escape.',
    items: [
      {
        image: 'city',
        title: 'A European weekend',
        description:
          'Discover charming cities, good food and fresh perspectives on a long weekend.',
        alt: 'Illustrative European canal city with pastel buildings and a stone bridge.',
      },
      {
        image: 'beach',
        title: 'Sun, sea & slower days',
        description:
          'Enjoy warm beaches, clear water and time to simply unwind.',
        alt: 'Illustrative quiet sandy beach with clear water, rocks and a parasol.',
      },
      {
        image: 'family',
        title: 'Time together',
        description:
          'Make memories with experiences for the whole family at a gentler pace.',
        alt: 'Illustrative family walking beside the sea in a small coastal town.',
      },
    ],
    note: 'AI-generated inspiration images. These are not bookable offers or a display of current availability.',
  },
  finalCta: {
    title: 'Ready to plan your next journey?',
    description:
      'Your initial enquiry will be free. You will receive a personal quote before deciding whether to proceed.',
  },
  footer: {
    description:
      'Personal help finding, planning and booking a journey that suits you.',
    explore: 'Explore',
    services: 'Our services',
    contact: 'Contact',
    contactPlaceholder: 'Contact details will be published before launch.',
    privacy: 'Privacy policy – coming soon',
    terms: 'Terms – coming soon',
  },
  comingSoon: {
    title: 'Your travel enquiry will start here soon',
    description:
      'The travel enquiry form is not open yet. In a later phase, you will be able to share your plans and receive a personal quote.',
    note: 'No enquiries or bookings are accepted on this page yet. The initial enquiry will be free. Research starts only after an accepted quote and a paid service fee.',
    aboutTitle: 'More about your travel planner soon',
    aboutDescription:
      'A personal introduction, name and portrait will be published before launch. In the meantime, discover personal travel planning on our homepage.',
  },
  notFound: {
    title: 'Page not found',
    description:
      'This address does not seem to exist. Explore our services and travel inspiration from the homepage.',
  },
  errors: {
    title: 'Something went wrong',
    description:
      'The page could not be displayed. Please reload and try again.',
    reload: 'Reload',
  },
  diagnostics: {
    title: 'Travel agency application is running',
    description: 'Connection diagnostics for the frontend and API.',
    checking: 'Checking API…',
    connected: 'API connected',
    unavailable: 'API is unavailable',
  },
} as const satisfies Translations;
