export function travelDay(date: string, language: string) {
  return new Intl.DateTimeFormat(language === 'sv' ? 'sv-SE' : 'en-GB', {
    dateStyle: 'full',
    timeZone: 'UTC',
  }).format(new Date(date + 'T12:00:00Z'));
}
export function travelTime(
  at: string | null,
  zone: string | null,
  language: string,
) {
  if (!at || !zone) return null;
  try {
    return new Intl.DateTimeFormat(language === 'sv' ? 'sv-SE' : 'en-GB', {
      hour: '2-digit',
      minute: '2-digit',
      timeZone: zone,
      timeZoneName: 'shortOffset',
    }).format(new Date(at));
  } catch {
    return null;
  }
}
export function localInput(at: string, zone: string) {
  const parts = new Intl.DateTimeFormat('en-GB', {
    timeZone: zone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hourCycle: 'h23',
    timeZoneName: 'longOffset',
  }).formatToParts(new Date(at));
  const value = (type: string) =>
    parts.find((p) => p.type === type)?.value ?? '';
  return {
    time: `${value('year')}-${value('month')}-${value('day')}T${value('hour')}:${value('minute')}`,
    offset: value('timeZoneName').replace('GMT', '') || '+00:00',
  };
}
