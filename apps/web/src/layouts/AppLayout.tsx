import { Outlet } from 'react-router';
import styles from './AppLayout.module.scss';

export function AppLayout() {
  return (
    <div className={styles.shell}>
      <header>Travel Agency · Utvecklingsmiljö</header>
      <main id="main-content">
        <Outlet />
      </main>
    </div>
  );
}
