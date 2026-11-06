import {
  useWebsite,
  applyFields,
} from '../features/business-management/contentContext';
import { serviceCodes, cardIds } from '../features/business-management/types';
import type { ServiceData } from '../features/business-management/types';
import type { Translations } from '../content/types';
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
  const { language } = useLanguage();
  const { documents } = useWebsite();
  const t = applyFields<Translations>(
    language === 'en' ? en : sv,
    documents,
    'shared',
    language,
  );
  for (let i = 0; i < serviceCodes.length; i++) {
    const data = documents['service:' + serviceCodes[i]] as
      ServiceData | undefined;
    if (data) {
      const x = t.services.items[i];
      if (x) {
        x.title = data[language].name;
        x.description = data[language].shortDescription;
        x.action = data[language].cta;
      }
    }
  }
  t.services.items.sort((a, b) => {
    const ai = cardIds.indexOf(a.id),
      bi = cardIds.indexOf(b.id);
    return (
      ((documents['service:' + serviceCodes[ai]] as ServiceData | undefined)
        ?.displayOrder ?? ai) -
      ((documents['service:' + serviceCodes[bi]] as ServiceData | undefined)
        ?.displayOrder ?? bi)
    );
  });
  return t;
}
