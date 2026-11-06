import { useContext } from 'react';
import { PreviewPageContext } from '../business-management/contentContext';
import { useLocation } from 'react-router';
import { WebsiteImage } from '../business-management/PublishedComponents';
import { Container } from '../../components/ui/Container';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { usePublicCopy } from './usePublicCopy';
import styles from './PublicPages.module.scss';
export function PageIntro({
  title,
  description,
}: {
  title: string;
  description: string;
}) {
  usePageMetadata(title, description);
  const copy = usePublicCopy();
  const location = useLocation();
  const page = useContext(PreviewPageContext) || location.pathname.slice(1);
  return (
    <section className={styles.intro}>
      <Container>
        <p className={styles.eyebrow}>{copy.eyebrow}</p>
        <h1>{title}</h1>
        <p className={styles.lead}>{description}</p>
        {page !== 'about' && <WebsiteImage page={page} />}
      </Container>
    </section>
  );
}
