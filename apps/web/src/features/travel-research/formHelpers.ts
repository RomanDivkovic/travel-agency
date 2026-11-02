import type { FlightLeg } from './types';
export function reorder<T>(items: T[], index: number, direction: number): T[] {
  const next = index + direction;
  if (next < 0 || next >= items.length) return items;
  const result = [...items];
  [result[index], result[next]] = [result[next]!, result[index]!];
  return result;
}
export const emptyLeg = (): FlightLeg => ({
  journey: 1,
  airline: '',
  flightNumber: '',
  departureAirport: '',
  arrivalAirport: '',
  departureAt: '',
  arrivalAt: '',
  departureTimeZone: '',
  arrivalTimeZone: '',
});

export function flightMetrics(legs: FlightLeg[]) {
  const groups = new Map<number, FlightLeg[]>();
  for (const leg of legs) {
    const key = leg.journey ?? 1;
    groups.set(key, [...(groups.get(key) ?? []), leg]);
  }
  return {
    connections: legs.length - groups.size,
    durationHours: [...groups.values()].reduce(
      (total, group) =>
        total +
        (Date.parse(group[group.length - 1]!.arrivalAt) -
          Date.parse(group[0]!.departureAt)) /
          3600000,
      0,
    ),
  };
}

export function formatFlightTime(
  raw: string,
  zone: string,
  language: 'sv' | 'en',
) {
  try {
    const offset = raw.match(/(Z|[+-]\d{2}:\d{2})$/)?.[1];
    if (!offset || !zone) return raw;
    // Preserve the frozen local time/offset even when browser timezone databases differ.
    const wall = new Date(raw.slice(0, -offset.length) + 'Z');
    return (
      new Intl.DateTimeFormat(language === 'sv' ? 'sv-SE' : 'en-GB', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        timeZone: 'UTC',
      }).format(wall) +
      ' UTC' +
      (offset === 'Z' ? '' : offset)
    );
  } catch {
    return raw;
  }
}

export function formatUtcTimestamp(raw: string, language: 'sv' | 'en') {
  try {
    return new Intl.DateTimeFormat(language === 'sv' ? 'sv-SE' : 'en-GB', {
      dateStyle: 'medium',
      timeStyle: 'medium',
      timeZone: 'UTC',
    }).format(new Date(raw));
  } catch {
    return raw;
  }
}
