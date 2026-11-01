import { hasAccommodation, hasFlights } from './config';
import type { TravelRequest, WizardData } from './model';
import { validateRequest } from './validation';
export function buildTravelRequest(
  data: WizardData,
  language: 'sv' | 'en',
): TravelRequest | undefined {
  if (validateRequest(data).length) return undefined;
  const base = {
    schemaVersion: 1 as const,
    language,
    destination: {
      ...(hasFlights(data.service)
        ? { origin: data.destination.origin.trim() }
        : {}),
      ...(data.destination.place.trim()
        ? { place: data.destination.place.trim() }
        : {}),
      flexible: data.destination.flexible,
      ...(data.service === 'complete' && data.destination.stops.trim()
        ? {
            additionalStops: data.destination.stops
              .split('\n')
              .map((s) => s.trim())
              .filter(Boolean),
          }
        : {}),
    },
    dates:
      data.dates.mode === 'fixed'
        ? {
            mode: 'fixed' as const,
            departure: data.dates.departure,
            ...(!(data.service === 'flights' && data.dates.oneWay)
              ? { return: data.dates.return }
              : {}),
          }
        : {
            mode: 'flexible' as const,
            earliest: data.dates.earliest,
            latest: data.dates.latest,
            durationDays: Number(data.dates.duration),
          },
    travelers: {
      adults: Number(data.travelers.adults),
      childrenAges: data.travelers.ages
        .slice(0, Number(data.travelers.children))
        .map(Number),
      ...(hasAccommodation(data.service)
        ? { rooms: Number(data.travelers.rooms) }
        : {}),
    },
    budget: { ...data.budget, amount: Number(data.budget.amount) },
    activities: data.additional.activities,
    ...(data.additional.message.trim()
      ? { specialRequests: data.additional.message.trim() }
      : {}),
    contact: {
      name: data.contact.name.trim(),
      email: data.contact.email.trim(),
      ...(data.contact.phone.trim()
        ? { phone: data.contact.phone.trim() }
        : {}),
      preferredMethod: data.contact.method,
    },
  };
  switch (data.service) {
    case 'flights':
      return {
        ...base,
        service: data.service,
        preferences: { flight: { ...data.flight } },
      };
    case 'accommodation':
      return {
        ...base,
        service: data.service,
        preferences: { accommodation: { ...data.accommodation } },
      };
    case 'flight-accommodation':
      return {
        ...base,
        service: data.service,
        preferences: {
          flight: { ...data.flight },
          accommodation: { ...data.accommodation },
        },
      };
    case 'complete':
      return {
        ...base,
        service: data.service,
        preferences: {
          flight: { ...data.flight },
          accommodation: { ...data.accommodation },
          trip: { ...data.trip, interests: [...data.trip.interests] },
        },
      };
  }
}
