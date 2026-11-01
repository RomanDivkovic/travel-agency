import type { CustomerQuotation, DraftInput, TaxTreatment } from './types';
export function parseMinor(value: string): number | undefined {
  if (!/^\d{1,9}(?:\.\d{1,2})?$/.test(value)) return;
  const [whole, part = ''] = value.split('.');
  const amount = Number(whole) * 100 + Number(part.padEnd(2, '0'));
  return Number.isSafeInteger(amount) && amount <= 10_000_000_000
    ? amount
    : undefined;
}
export function minorInput(value: number) {
  return `${Math.floor(value / 100)}.${String(value % 100).padStart(2, '0')}`;
}
export function calculatePreview(
  lines: DraftInput['lines'],
  mode: TaxTreatment,
) {
  let subtotal = 0,
    tax = 0,
    total = 0;
  if (lines.length < 1 || lines.length > 20) return;
  for (const line of lines) {
    const amount = line.quantity * line.unitPriceMinorUnits;
    if (
      !Number.isSafeInteger(amount) ||
      line.quantity < 1 ||
      line.quantity > 100 ||
      !Number.isInteger(line.quantity) ||
      line.unitPriceMinorUnits < 0 ||
      amount > 10_000_000_000 ||
      !Number.isInteger(line.taxRateBasisPoints) ||
      line.taxRateBasisPoints < 0 ||
      line.taxRateBasisPoints > 10000 ||
      ((mode === 'Unconfirmed' || mode === 'NoVat') &&
        line.taxRateBasisPoints !== 0)
    )
      return;
    const denominator =
      mode === 'VatIncluded' ? 10000 + line.taxRateBasisPoints : 10000;
    const lineTax =
      mode === 'VatIncluded' || mode === 'VatExclusive'
        ? Math.floor(
            (amount * line.taxRateBasisPoints + denominator / 2) / denominator,
          )
        : 0;
    subtotal += mode === 'VatIncluded' ? amount - lineTax : amount;
    tax += lineTax;
    total += mode === 'VatIncluded' ? amount : amount + lineTax;
  }
  return total <= 10_000_000_000 ? { subtotal, tax, total } : undefined;
}
export function formatMoney(
  value: number,
  currency: CustomerQuotation['currency'],
  language: 'sv' | 'en',
) {
  return new Intl.NumberFormat(language === 'sv' ? 'sv-SE' : 'en-GB', {
    style: 'currency',
    currency,
  }).format(value / 100);
}
export function localDateTime(value: string) {
  const date = new Date(value);
  const two = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${two(date.getMonth() + 1)}-${two(date.getDate())}T${two(date.getHours())}:${two(date.getMinutes())}`;
}
