import { hasAccommodation, hasFlights, isService, wizardSteps } from './config';
import type { WizardData, StepId } from './model';
export type ErrorCode =
  | 'required'
  | 'length'
  | 'date'
  | 'dateOrder'
  | 'duration'
  | 'adults'
  | 'children'
  | 'age'
  | 'rooms'
  | 'budget'
  | 'email'
  | 'phone'
  | 'choice';
export type FormErrors = Record<string, ErrorCode>;
export function localToday() {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}
export function validDate(value: string) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const time = Date.parse(`${value}T00:00:00Z`);
  return (
    Number.isFinite(time) && new Date(time).toISOString().slice(0, 10) === value
  );
}
export function integer(value: string, min: number, max: number) {
  return /^\d+$/.test(value) && Number(value) >= min && Number(value) <= max;
}
export function validateStep(
  step: StepId,
  data: WizardData,
  today = localToday(),
): FormErrors {
  const errors: FormErrors = {};
  function text(key: string, value: string, max: number, required = true) {
    if (required && !value.trim()) errors[key] = 'required';
    else if (value.length > max) errors[key] = 'length';
  }
  function date(key: string, value: string) {
    if (
      !validDate(value) ||
      value < today ||
      value > `${Number(today.slice(0, 4)) + 10}${today.slice(4)}`
    )
      errors[key] = 'date';
  }
  if (step === 'service' && !isService(data.service))
    errors.service = 'required';
  if (step === 'destination') {
    if (hasFlights(data.service)) text('origin', data.destination.origin, 150);
    text('place', data.destination.place, 150, !data.destination.flexible);
    if (data.service === 'complete')
      text('stops', data.destination.stops, 500, false);
  }
  if (step === 'dates') {
    const d = data.dates;
    if (d.mode === 'fixed') {
      date('departure', d.departure);
      if (!(data.service === 'flights' && d.oneWay)) {
        date('return', d.return);
        if (
          !errors.departure &&
          !errors.return &&
          (d.return < d.departure ||
            (hasAccommodation(data.service) && d.return === d.departure))
        )
          errors.return = 'dateOrder';
      }
    } else if (d.mode === 'flexible') {
      date('earliest', d.earliest);
      date('latest', d.latest);
      if (
        !errors.earliest &&
        !errors.latest &&
        (d.latest < d.earliest ||
          (hasAccommodation(data.service) && d.latest === d.earliest))
      )
        errors.latest = 'dateOrder';
      if (
        !integer(d.duration, 1, 90) ||
        (!errors.earliest &&
          !errors.latest &&
          Number(d.duration) >
            (Date.parse(d.latest) - Date.parse(d.earliest)) / 86400000 +
              (hasAccommodation(data.service) ? 0 : 1))
      )
        errors.duration = 'duration';
    } else errors.dateMode = 'choice';
  }
  if (step === 'travelers') {
    const t = data.travelers;
    if (!integer(t.adults, 1, 20)) errors.adults = 'adults';
    if (
      !integer(t.children, 0, 10) ||
      t.ages.length !== Number(t.children) ||
      Number(t.adults) + Number(t.children) > 20
    )
      errors.children = 'children';
    if (!errors.children)
      for (let i = 0; i < Number(t.children); i++)
        if (!integer(t.ages[i] ?? '', 0, 17)) errors[`age-${i}`] = 'age';
    if (
      hasAccommodation(data.service) &&
      !integer(t.rooms, 1, Math.min(20, Number(t.adults) + Number(t.children)))
    )
      errors.rooms = 'rooms';
  }
  if (step === 'budget') {
    if (
      !/^\d+(\.\d{1,2})?$/.test(data.budget.amount) ||
      Number(data.budget.amount) <= 0 ||
      Number(data.budget.amount) > 100000000
    )
      errors.amount = 'budget';
    if (!['SEK', 'EUR', 'USD'].includes(data.budget.currency))
      errors.currency = 'choice';
    if (!['total', 'per-person'].includes(data.budget.scope))
      errors.scope = 'choice';
  }
  if (
    step === 'flightPreferences' &&
    (!['balanced', 'price', 'direct'].includes(data.flight.priority) ||
      !['cabin', 'checked', 'none'].includes(data.flight.baggage))
  )
    errors.priority = 'choice';
  if (
    step === 'accommodationPreferences' &&
    !['hotel', 'apartment', 'either'].includes(data.accommodation.type)
  )
    errors.accommodationType = 'choice';
  if (
    step === 'tripPreferences' &&
    (!['relaxed', 'balanced', 'active'].includes(data.trip.pace) ||
      data.trip.interests.some(
        (x) => !['beach', 'culture', 'nature', 'food'].includes(x),
      ))
  )
    errors.pace = 'choice';
  if (step === 'additional')
    text('special', data.additional.message, 2000, false);
  if (step === 'contact') {
    const c = data.contact;
    text('name', c.name, 100);
    text('email', c.email, 254);
    if (!errors.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(c.email.trim()))
      errors.email = 'email';
    if (c.method === 'phone' || c.phone.trim()) {
      const digits = c.phone.replace(/\D/g, '');
      if (
        !/^[+\d\s().-]+$/.test(c.phone) ||
        digits.length < 6 ||
        digits.length > 15
      )
        errors.phone = 'phone';
    }
    if (!['email', 'phone'].includes(c.method)) errors.contactMethod = 'choice';
  }
  return errors;
}
export function validateRequest(
  data: WizardData,
  today = localToday(),
): { step: StepId; errors: FormErrors }[] {
  return wizardSteps(data.service)
    .filter((step) => step !== 'review')
    .map((step) => ({ step, errors: validateStep(step, data, today) }))
    .filter((result) => Object.keys(result.errors).length > 0);
}
