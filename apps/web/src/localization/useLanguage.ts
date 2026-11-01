import { createContext, useContext } from 'react';
import { sv } from '../content/sv';
import { en } from '../content/en';
export type Language = 'sv' | 'en';
export const languageStorageKey = 'travel-agency.language.v1';
export const LanguageContext = createContext({
  language: 'sv' as Language,
  setLanguage: (_language: Language) => {
    void _language;
  },
});
export function readLanguage(): Language {
  try {
    return localStorage.getItem(languageStorageKey) === 'en' ? 'en' : 'sv';
  } catch {
    return 'sv';
  }
}
export function useLanguage() {
  return useContext(LanguageContext);
}
export function useTranslations() {
  return useLanguage().language === 'en' ? en : sv;
}
