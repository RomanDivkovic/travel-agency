// Only a public mailbox belongs in Vite configuration. Never put credentials here.
export function validContactEmail(value: unknown): string | undefined {
  if (typeof value !== 'string') return undefined;
  const email = value.trim();
  return /^[A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+@[A-Za-z0-9](?:[A-Za-z0-9.-]*[A-Za-z0-9])?\.[A-Za-z]{2,}$/.test(
    email,
  )
    ? email
    : undefined;
}
export const contactEmail = validContactEmail(
  import.meta.env.VITE_CONTACT_EMAIL,
);
