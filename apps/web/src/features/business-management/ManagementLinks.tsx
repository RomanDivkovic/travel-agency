import { Link } from 'react-router';
import { useCmsCopy } from './copy';
import styles from './Management.module.scss';
export function ManagementLinks() {
  const t = useCmsCopy();
  return (
    <nav aria-label={t.settings} className={styles.actions}>
      <Link to="/admin/settings/business">{t.business}</Link>
      <Link to="/admin/settings/services">{t.services}</Link>
      <Link to="/admin/content">{t.pages}</Link>
      <Link to="/admin/content/faqs">{t.faqs}</Link>
      <Link to="/admin/content/policies">{t.policies}</Link>
      <Link to="/admin/content/media">{t.media}</Link>
      <Link to="/admin/settings/preferences">{t.preferences}</Link>
    </nav>
  );
}
