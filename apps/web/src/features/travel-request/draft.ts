import { emptyWizard, type WizardData } from './model';
import { isService } from './config';
export const draftKey = 'travel-agency.request-draft.v1';
export const draftLifetime = 7 * 24 * 60 * 60 * 1000;
type SafeDraft = Pick<
  WizardData,
  | 'service'
  | 'dates'
  | 'travelers'
  | 'budget'
  | 'flight'
  | 'accommodation'
  | 'trip'
> & { activities: boolean };
// React observes the outcome of external storage writes without effect-driven UI setters.
let writeAvailable = true;
const storageListeners = new Set<() => void>();
export function subscribeDraftStorage(listener: () => void) {
  storageListeners.add(listener);
  return () => {
    storageListeners.delete(listener);
  };
}
export function draftStorageAvailable() {
  return writeAvailable;
}
function reportWrite(available: boolean) {
  if (writeAvailable !== available) {
    writeAvailable = available;
    for (const listener of storageListeners) listener();
  }
  return available;
}
export function permittedDraft(data: WizardData): SafeDraft {
  return {
    service: data.service,
    activities: data.additional.activities,
    dates: {
      mode: data.dates.mode,
      oneWay: data.dates.oneWay,
      departure: data.dates.departure,
      return: data.dates.return,
      earliest: data.dates.earliest,
      latest: data.dates.latest,
      duration: data.dates.duration,
    },
    travelers: {
      adults: data.travelers.adults,
      children: data.travelers.children,
      ages: [...data.travelers.ages],
      rooms: data.travelers.rooms,
    },
    budget: {
      amount: data.budget.amount,
      currency: data.budget.currency,
      scope: data.budget.scope,
    },
    flight: {
      priority: data.flight.priority,
      baggage: data.flight.baggage,
      flexibleAirports: data.flight.flexibleAirports,
    },
    accommodation: {
      type: data.accommodation.type,
      breakfast: data.accommodation.breakfast,
      pool: data.accommodation.pool,
      familyFriendly: data.accommodation.familyFriendly,
    },
    trip: { pace: data.trip.pace, interests: [...data.trip.interests] },
  };
}
function record(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
// Rebuild only the known shape. Extra keys, free text and contact data never enter state.
function parseShape(value: unknown, template: unknown): unknown {
  if (typeof template === 'string')
    return typeof value === 'string' && value.length <= 32 ? value : undefined;
  if (typeof template === 'boolean')
    return typeof value === 'boolean' ? value : undefined;
  if (Array.isArray(template))
    return Array.isArray(value) &&
      value.length <= 10 &&
      value.every((x) => typeof x === 'string' && x.length <= 32)
      ? [...value]
      : undefined;
  if (!record(value) || !record(template)) return undefined;
  const result: Record<string, unknown> = {};
  for (const key of Object.keys(template)) {
    const parsed = parseShape(value[key], template[key]);
    if (parsed === undefined) return undefined;
    result[key] = parsed;
  }
  return result;
}
export function parseDraft(
  raw: string | null,
  now = Date.now(),
): WizardData | undefined {
  if (!raw || raw.length > 10000) return undefined;
  try {
    const value: unknown = JSON.parse(raw);
    if (
      !record(value) ||
      value.version !== 1 ||
      typeof value.savedAt !== 'number' ||
      !Number.isFinite(value.savedAt) ||
      now - value.savedAt > draftLifetime ||
      value.savedAt > now + 300000
    )
      return undefined;
    const parsed = parseShape(value.data, permittedDraft(emptyWizard())) as
      SafeDraft | undefined;
    if (
      !parsed ||
      (parsed.service !== '' && !isService(parsed.service)) ||
      !['fixed', 'flexible'].includes(parsed.dates.mode) ||
      !['SEK', 'EUR', 'USD'].includes(parsed.budget.currency) ||
      !['total', 'per-person'].includes(parsed.budget.scope) ||
      !['balanced', 'price', 'direct'].includes(parsed.flight.priority) ||
      !['cabin', 'checked', 'none'].includes(parsed.flight.baggage) ||
      !['hotel', 'apartment', 'either'].includes(parsed.accommodation.type) ||
      !['relaxed', 'balanced', 'active'].includes(parsed.trip.pace) ||
      parsed.trip.interests.some(
        (x) => !['beach', 'culture', 'nature', 'food'].includes(x),
      )
    )
      return undefined;
    const { activities, ...safe } = parsed;
    return {
      ...emptyWizard(),
      ...safe,
      additional: { activities, message: '' },
    };
  } catch {
    return undefined;
  }
}
export function loadDraft() {
  try {
    return {
      data: parseDraft(localStorage.getItem(draftKey)),
      available: true,
    };
  } catch {
    return { data: undefined, available: false };
  }
}
export function storeDraft(data: WizardData, enabled: boolean): boolean {
  try {
    if (enabled)
      localStorage.setItem(
        draftKey,
        JSON.stringify({
          version: 1,
          savedAt: Date.now(),
          data: permittedDraft(data),
        }),
      );
    else localStorage.removeItem(draftKey);
    return reportWrite(true);
  } catch {
    return reportWrite(false);
  }
}
