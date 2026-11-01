import { useLanguage } from '../../localization/useLanguage';
export function useMoney() {
  const { language } = useLanguage();
  return (amount: number, currency: string) =>
    new Intl.NumberFormat(language === 'sv' ? 'sv-SE' : 'en-GB', {
      style: 'currency',
      currency,
    }).format(amount / 100);
}
