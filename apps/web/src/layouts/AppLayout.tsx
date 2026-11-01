import { Outlet } from 'react-router';
import { useTranslations } from '../localization/useLanguage';
import { Header } from '../components/layout/Header';
import { Footer } from '../components/layout/Footer';
import styles from './AppLayout.module.scss';

export function AppLayout() {
  const t = useTranslations();
  return (
    <>
      <a href="#main-content" className={styles.skip}>
        {t.navigation.skip}
      </a>
      <Header />
      <main id="main-content" tabIndex={-1}>
        <Outlet />
      </main>
      <Footer />
    </>
  );
}
