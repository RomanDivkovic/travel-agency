import { Outlet } from 'react-router';
import { sv } from '../content/sv';
import { Header } from '../components/layout/Header';
import { Footer } from '../components/layout/Footer';
import styles from './AppLayout.module.scss';

export function AppLayout() {
  return (
    <>
      <a href="#main-content" className={styles.skip}>
        {sv.navigation.skip}
      </a>
      <Header />
      <main id="main-content" tabIndex={-1}>
        <Outlet />
      </main>
      <Footer />
    </>
  );
}
