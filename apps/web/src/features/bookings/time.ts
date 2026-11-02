// Fields are explicitly labelled UTC. Preserve entered seconds/milliseconds instead of appending another seconds segment.
export function utcInput(raw: string): string {
  return new Date(raw + 'Z').toISOString();
}
