import { useEffect, useState, type ReactNode } from 'react';
import {
  LanguageContext,
  readLanguage,
  languageStorageKey,
  type Language,
} from './useLanguage';
export function LanguageProvider({ children }: { children: ReactNode }) {
  const [language, updateLanguage] = useState<Language>(readLanguage);
  useEffect(() => {
    document.documentElement.lang = language;
  }, [language]);
  function setLanguage(next: Language) {
    updateLanguage(next);
    try {
      localStorage.setItem(languageStorageKey, next);
    } catch {
      /* Switching still works when storage is blocked. */
    }
  }
  return (
    <LanguageContext value={{ language, setLanguage }}>
      {children}
    </LanguageContext>
  );
}
