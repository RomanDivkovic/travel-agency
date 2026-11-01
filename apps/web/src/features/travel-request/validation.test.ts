import { expect, it } from 'vitest';
import { services, emptyWizard } from './model';
import { wizardSteps, serviceWizardLink } from './config';
import { wizardReducer } from './reducer';
import { buildTravelRequest } from './payload';
import {
  localToday,
  validDate,
  validateRequest,
  validateStep,
} from './validation';
import { validWizard } from './testData';
for (const service of services)
  it(`builds a complete validated ${service} payload with only relevant fields`, () => {
    const data = validWizard(service);
    expect(validateRequest(data)).toEqual([]);
    const result = buildTravelRequest(data, 'en');
    expect(result?.service).toBe(service);
    expect(result?.schemaVersion).toBe(1);
    expect(result?.language).toBe('en');
    expect(result?.budget.amount).toBe(25000.5);
    expect(result?.travelers.childrenAges).toEqual([7]);
    expect(result?.contact.name).toBe('Anna Andersson');
    expect(result?.contact).not.toHaveProperty('phone');
    expect(result?.preferences).toHaveProperty(
      service === 'accommodation' ? 'accommodation' : 'flight',
    );
    if (service === 'flights') {
      expect(result?.preferences).not.toHaveProperty('accommodation');
      expect(result?.travelers).not.toHaveProperty('rooms');
    }
    if (service === 'accommodation') {
      expect(result?.preferences).not.toHaveProperty('flight');
      expect(result?.destination).not.toHaveProperty('origin');
    }
    if (service !== 'complete') {
      expect(result?.preferences).not.toHaveProperty('trip');
      expect(result?.destination).not.toHaveProperty('additionalStops');
    } else
      expect(result?.destination.additionalStops).toEqual(['Porto', 'Coimbra']);
  });
it('configures only the service-relevant preference steps and maps all card entry links', () => {
  expect(wizardSteps('flights')).not.toContain('accommodationPreferences');
  expect(wizardSteps('accommodation')).not.toContain('flightPreferences');
  expect(wizardSteps('complete')).toContain('tripPreferences');
  expect(wizardSteps('flight-accommodation')).not.toContain('tripPreferences');
  expect(
    ['flyg', 'boende', 'flyg-boende', 'semester'].map(serviceWizardLink),
  ).toEqual(services.map((service) => `/plan-your-trip?service=${service}`));
  expect(serviceWizardLink('unknown')).toBe('/plan-your-trip');
});
it('rejects an incomplete request and never constructs an invalid payload', () => {
  expect(validateRequest(emptyWizard())).not.toHaveLength(0);
  expect(buildTravelRequest(emptyWizard(), 'sv')).toBeUndefined();
});
it('requires a departure location only for flight journeys and supports open destinations', () => {
  const data = validWizard('flights');
  data.destination.origin = ' ';
  data.destination.place = '';
  expect(validateStep('destination', data)).toEqual({
    origin: 'required',
    place: 'required',
  });
  data.destination.flexible = true;
  expect(validateStep('destination', data)).toEqual({ origin: 'required' });
  data.service = 'accommodation';
  expect(validateStep('destination', data)).toEqual({});
});
it('validates real calendar dates, future limits and ordering', () => {
  expect(validDate('2028-02-29')).toBe(true);
  for (const value of ['2027-02-29', '2028-02-30', '2028-13-01', 'no date'])
    expect(validDate(value)).toBe(false);
  const data = validWizard();
  data.dates.departure = '1900-01-01';
  expect(validateStep('dates', data)).toHaveProperty('departure', 'date');
  data.dates.departure = '9999-01-01';
  expect(validateStep('dates', data)).toHaveProperty('departure', 'date');
  data.dates = validWizard().dates;
  data.dates.return = data.dates.departure;
  expect(validateStep('dates', data)).toHaveProperty('return', 'dateOrder');
  data.service = 'flights';
  expect(validateStep('dates', data)).toEqual({});
});
it('supports one-way flights without leaking a hidden return date', () => {
  const data = validWizard('flights');
  data.dates.oneWay = true;
  data.dates.return = '';
  expect(validateStep('dates', data)).toEqual({});
  expect(buildTravelRequest(data, 'sv')?.dates).not.toHaveProperty('return');
  data.service = 'accommodation';
  expect(validateStep('dates', data)).toHaveProperty('return');
});
it('validates flexible windows and creates a discriminated flexible date payload', () => {
  const data = validWizard();
  data.dates.mode = 'flexible';
  expect(validateStep('dates', data)).toEqual({});
  expect(buildTravelRequest(data, 'sv')?.dates).toEqual({
    mode: 'flexible',
    earliest: data.dates.earliest,
    latest: data.dates.latest,
    durationDays: 7,
  });
  data.dates.duration = '91';
  expect(validateStep('dates', data)).toHaveProperty('duration');
  data.dates.duration = '30';
  expect(validateStep('dates', data)).toHaveProperty('duration');
  data.dates.latest = data.dates.earliest;
  expect(validateStep('dates', data)).toHaveProperty('latest');
});
it('requires integer counts, all child ages and a sensible number of rooms', () => {
  const data = validWizard();
  data.travelers = { adults: '0', children: '11', ages: [], rooms: '0' };
  expect(validateStep('travelers', data)).toMatchObject({
    adults: 'adults',
    children: 'children',
    rooms: 'rooms',
  });
  data.travelers = { adults: '20', children: '1', ages: ['18'], rooms: '2' };
  expect(validateStep('travelers', data)).toMatchObject({
    children: 'children',
  });
  data.travelers = { adults: '1', children: '2', ages: ['7', ''], rooms: '4' };
  expect(validateStep('travelers', data)).toMatchObject({
    'age-1': 'age',
    rooms: 'rooms',
  });
  data.travelers.ages = ['0', '17'];
  data.travelers.rooms = '2';
  expect(validateStep('travelers', data)).toEqual({});
  data.travelers.adults = '1.5';
  expect(validateStep('travelers', data)).toHaveProperty('adults');
});
for (const amount of [
  '',
  '0',
  '-1',
  '1e5',
  'Infinity',
  '1.234',
  '100000001',
  'abc',
])
  it(`rejects invalid budget ${JSON.stringify(amount)}`, () => {
    const data = validWizard();
    data.budget.amount = amount;
    expect(validateStep('budget', data)).toHaveProperty('amount', 'budget');
    expect(buildTravelRequest(data, 'sv')).toBeUndefined();
  });
it('validates lengths, whitespace, email and conditional phone requirements', () => {
  const data = validWizard();
  data.contact.name = ' ';
  data.contact.email = 'anna@';
  data.contact.method = 'phone';
  expect(validateStep('contact', data)).toMatchObject({
    name: 'required',
    email: 'email',
    phone: 'phone',
  });
  data.contact = {
    name: 'Anna',
    email: 'a@example.com',
    method: 'phone',
    phone: '+46 (70) 123-45-67',
  };
  expect(validateStep('contact', data)).toEqual({});
  data.contact.phone = 'hello';
  expect(validateStep('contact', data)).toHaveProperty('phone');
  data.additional.message = 'x'.repeat(2001);
  expect(validateStep('additional', data)).toHaveProperty('special', 'length');
});
it('retains shared answers while clearing service-specific answers on a change', () => {
  const data = validWizard();
  data.flight.priority = 'direct';
  data.accommodation.pool = true;
  data.trip.interests = ['nature'];
  data.dates.oneWay = true;
  const result = wizardReducer(data, {
    type: 'service',
    value: 'accommodation',
  });
  expect(result.destination).toEqual(data.destination);
  expect(result.contact).toEqual(data.contact);
  expect(result.flight.priority).toBe('balanced');
  expect(result.accommodation.pool).toBe(false);
  expect(result.trip.interests).toEqual([]);
  expect(result.dates.oneWay).toBe(false);
  expect(wizardReducer(data, { type: 'service', value: 'complete' })).toBe(
    data,
  );
});
it('resizes child age entries without losing surviving answers', () => {
  const data = validWizard();
  const grown = wizardReducer(data, {
    type: 'update',
    group: 'travelers',
    value: { children: '3' },
  });
  expect(grown.travelers.ages).toEqual(['7', '', '']);
  const shrunk = wizardReducer(grown, {
    type: 'update',
    group: 'travelers',
    value: { children: '0' },
  });
  expect(shrunk.travelers.ages).toEqual([]);
  expect(data.travelers.ages).toEqual(['7']);
});
it('returns an independent normalized payload without mutating form answers', () => {
  const data = validWizard();
  const result = buildTravelRequest(data, 'sv');
  if (result?.service === 'complete') {
    result.preferences.trip.interests.push('nature');
    expect(data.trip.interests).toEqual([]);
  }
  expect(data.destination.place).toBe(' Lisbon ');
  expect(localToday()).toMatch(/^\d{4}-\d{2}-\d{2}$/);
});
