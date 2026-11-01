import { useLanguage } from '../../localization/useLanguage';
import styles from './Header.module.scss';

export function LanguageSwitcher() {
  const { language, setLanguage } = useLanguage();
  return (
    <label className={styles.language}>
      <span className={styles.languageLabel}>
        {language === 'sv' ? 'Språk' : 'Language'}
      </span>
      <select
        aria-label={language === 'sv' ? 'Språk' : 'Language'}
        value={language}
        onChange={(event) =>
          setLanguage(event.target.value === 'en' ? 'en' : 'sv')
        }
      >
        <option value="sv" lang="sv">
          Svenska
        </option>
        <option value="en" lang="en">
          English
        </option>
      </select>
    </label>
  );
}
