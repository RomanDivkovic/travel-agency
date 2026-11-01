export const services = [
  'flights',
  'accommodation',
  'flight-accommodation',
  'complete',
] as const;
export type Service = (typeof services)[number];
export type Currency = 'SEK' | 'EUR' | 'USD';
export type StepId =
  | 'service'
  | 'destination'
  | 'dates'
  | 'travelers'
  | 'budget'
  | 'flightPreferences'
  | 'accommodationPreferences'
  | 'tripPreferences'
  | 'additional'
  | 'contact'
  | 'review';
export interface WizardData {
  service: Service | '';
  destination: {
    origin: string;
    place: string;
    flexible: boolean;
    stops: string;
  };
  dates: {
    mode: 'fixed' | 'flexible';
    oneWay: boolean;
    departure: string;
    return: string;
    earliest: string;
    latest: string;
    duration: string;
  };
  travelers: {
    adults: string;
    children: string;
    ages: string[];
    rooms: string;
  };
  budget: { amount: string; currency: Currency; scope: 'total' | 'per-person' };
  flight: {
    priority: 'balanced' | 'price' | 'direct';
    baggage: 'cabin' | 'checked' | 'none';
    flexibleAirports: boolean;
  };
  accommodation: {
    type: 'hotel' | 'apartment' | 'either';
    breakfast: boolean;
    pool: boolean;
    familyFriendly: boolean;
  };
  trip: {
    pace: 'relaxed' | 'balanced' | 'active';
    interests: ('beach' | 'culture' | 'nature' | 'food')[];
  };
  additional: { activities: boolean; message: string };
  contact: {
    name: string;
    email: string;
    phone: string;
    method: 'email' | 'phone';
  };
}
export function emptyWizard(service: Service | '' = ''): WizardData {
  return {
    service,
    destination: { origin: '', place: '', flexible: false, stops: '' },
    dates: {
      mode: 'fixed',
      oneWay: false,
      departure: '',
      return: '',
      earliest: '',
      latest: '',
      duration: '',
    },
    travelers: { adults: '1', children: '0', ages: [], rooms: '1' },
    budget: { amount: '', currency: 'SEK', scope: 'total' },
    flight: { priority: 'balanced', baggage: 'cabin', flexibleAirports: false },
    accommodation: {
      type: 'either',
      breakfast: false,
      pool: false,
      familyFriendly: false,
    },
    trip: { pace: 'balanced', interests: [] },
    additional: { activities: false, message: '' },
    contact: { name: '', email: '', phone: '', method: 'email' },
  };
}
export type TravelDates =
  | { mode: 'fixed'; departure: string; return?: string }
  | {
      mode: 'flexible';
      earliest: string;
      latest: string;
      durationDays: number;
    };
type RequestBase = {
  schemaVersion: 1;
  language: 'sv' | 'en';
  destination: {
    origin?: string;
    place?: string;
    flexible: boolean;
    additionalStops?: string[];
  };
  dates: TravelDates;
  travelers: { adults: number; childrenAges: number[]; rooms?: number };
  budget: { amount: number; currency: Currency; scope: 'total' | 'per-person' };
  activities: boolean;
  specialRequests?: string;
  contact: {
    name: string;
    email: string;
    phone?: string;
    preferredMethod: 'email' | 'phone';
  };
};
export type TravelRequest = RequestBase &
  (
    | { service: 'flights'; preferences: { flight: WizardData['flight'] } }
    | {
        service: 'accommodation';
        preferences: { accommodation: WizardData['accommodation'] };
      }
    | {
        service: 'flight-accommodation';
        preferences: {
          flight: WizardData['flight'];
          accommodation: WizardData['accommodation'];
        };
      }
    | {
        service: 'complete';
        preferences: {
          flight: WizardData['flight'];
          accommodation: WizardData['accommodation'];
          trip: WizardData['trip'];
        };
      }
  );
