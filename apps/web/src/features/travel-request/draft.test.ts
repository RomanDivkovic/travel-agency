import { expect, it, vi } from 'vitest';
import {
  draftKey,
  draftLifetime,
  loadDraft,
  parseDraft,
  permittedDraft,
  storeDraft,
} from './draft';
import { validWizard } from './testData';
function raw(
  data: unknown = permittedDraft(validWizard()),
  savedAt = Date.now(),
  version = 1,
) {
  return JSON.stringify({ version, savedAt, data });
}
it('persists only opted-in structured selections and excludes all free text and contact details', () => {
  const data = validWizard();
  expect(storeDraft(data, false)).toBe(true);
  expect(localStorage.getItem(draftKey)).toBeNull();
  expect(storeDraft(data, true)).toBe(true);
  const stored = localStorage.getItem(draftKey)!;
  for (const value of [
    'Anna',
    'anna@',
    'Stockholm',
    'Lisbon',
    'Porto',
    'station',
    'contact',
    'destination',
    'message',
  ])
    expect(stored).not.toContain(value);
  expect(JSON.parse(stored).data.activities).toBe(true);
  expect(loadDraft().data?.budget).toEqual(data.budget);
});
it('restores permitted answers while leaving excluded data empty and ignores injected extra fields', () => {
  const permitted = {
    ...permittedDraft(validWizard()),
    contact: { email: 'secret@example.com' },
    destination: { place: 'Secret' },
  };
  const result = parseDraft(raw(permitted));
  expect(result?.service).toBe('complete');
  expect(result?.contact.email).toBe('');
  expect(result?.destination.place).toBe('');
  expect(result?.additional.message).toBe('');
  expect(result?.additional.activities).toBe(true);
});
it('rejects malformed, oversized, expired and future-version drafts', () => {
  for (const value of [
    null,
    '{bad',
    'null',
    '[]',
    'x'.repeat(10001),
    raw(undefined, Date.now() - draftLifetime - 1),
    raw(undefined, Date.now(), 2),
    raw(undefined, Date.now() + 600000),
  ])
    expect(parseDraft(value)).toBeUndefined();
});
it('rejects invalid runtime types and invalid enum values without throwing', () => {
  const base = permittedDraft(validWizard());
  expect(parseDraft(raw({ ...base, service: 'unknown' }))).toBeUndefined();
  expect(
    parseDraft(
      JSON.stringify({
        version: 1,
        savedAt: Date.now(),
        data: { ...base, travelers: { adults: { bad: true } } },
      }),
    ),
  ).toBeUndefined();
  expect(
    parseDraft(
      JSON.stringify({
        version: 1,
        savedAt: Date.now(),
        data: { ...base, flight: { ...base.flight, priority: 'dangerous' } },
      }),
    ),
  ).toBeUndefined();
  expect(
    parseDraft(
      JSON.stringify({
        version: 1,
        savedAt: Date.now(),
        data: { ...base, trip: { ...base.trip, interests: ['unknown'] } },
      }),
    ),
  ).toBeUndefined();
});
it('clears the stored draft when saving is turned off', () => {
  storeDraft(validWizard(), true);
  expect(storeDraft(validWizard(), false)).toBe(true);
  expect(localStorage.getItem(draftKey)).toBeNull();
});
it('handles blocked storage and quota errors without crashing', () => {
  const read = vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
    throw Error('blocked');
  });
  expect(loadDraft()).toEqual({ data: undefined, available: false });
  read.mockRestore();
  const write = vi
    .spyOn(Storage.prototype, 'setItem')
    .mockImplementation(() => {
      throw Error('quota');
    });
  expect(storeDraft(validWizard(), true)).toBe(false);
  write.mockRestore();
  const remove = vi
    .spyOn(Storage.prototype, 'removeItem')
    .mockImplementation(() => {
      throw Error('blocked');
    });
  expect(storeDraft(validWizard(), false)).toBe(false);
  remove.mockRestore();
});

it('does not accidentally persist new or unexpected nested form properties', () => {
  const data = validWizard();
  for (const group of [
    data.dates,
    data.travelers,
    data.budget,
    data.flight,
    data.accommodation,
    data.trip,
  ])
    Object.assign(group, { privateNotes: 'must-never-persist' });
  storeDraft(data, true);
  expect(localStorage.getItem(draftKey)).not.toContain('privateNotes');
  expect(localStorage.getItem(draftKey)).not.toContain('must-never-persist');
});
