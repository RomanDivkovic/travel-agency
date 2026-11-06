import { useContent } from '../../features/business-management/contentContext';
import type { BusinessData } from '../../features/business-management/types';
import { Link } from 'react-router';
import { branding } from '../../app/config/branding';
import { routes } from '../../app/config/routes';
import { Icon } from '../ui/Icon';
import styles from './BrandLogo.module.scss';

export function BrandLogo() {
  const business = useContent<BusinessData>('business');
  return (
    <Link to={routes.home} className={styles.logo}>
      <Icon name="compass" size={38} />
      <span>{business?.businessName || branding.name}</span>
    </Link>
  );
}
