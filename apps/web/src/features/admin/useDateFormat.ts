import { useLanguage } from '../../localization/useLanguage';
export function useDateFormat() {
  const { language } = useLanguage();
  return (value: string) =>
    new Intl.DateTimeFormat(language === 'sv' ? 'sv-SE' : 'en-GB', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
}
