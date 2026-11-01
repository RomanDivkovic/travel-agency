import { routes } from '../../app/config/routes';
import { services, type Service, type StepId } from './model';
export function isService(value: unknown): value is Service {
  return services.some((service) => service === value);
}
export function hasFlights(service: string) {
  return ['flights', 'flight-accommodation', 'complete'].includes(service);
}
export function hasAccommodation(service: string) {
  return ['accommodation', 'flight-accommodation', 'complete'].includes(
    service,
  );
}
export function wizardSteps(service: string): StepId[] {
  return [
    'service',
    'destination',
    'dates',
    'travelers',
    'budget',
    ...(hasFlights(service) ? ['flightPreferences' as const] : []),
    ...(hasAccommodation(service) ? ['accommodationPreferences' as const] : []),
    ...(service === 'complete' ? ['tripPreferences' as const] : []),
    'additional',
    'contact',
    'review',
  ];
}
const cardServices: Record<string, Service> = {
  flyg: 'flights',
  boende: 'accommodation',
  'flyg-boende': 'flight-accommodation',
  semester: 'complete',
};
export function serviceWizardLink(cardId: string) {
  const service = cardServices[cardId];
  return service ? `${routes.planTrip}?service=${service}` : routes.planTrip;
}
