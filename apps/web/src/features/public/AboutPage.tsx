import { WebsiteImage } from '../business-management/PublishedComponents';
import { useContent } from '../business-management/contentContext';
import type { PageData } from '../business-management/types';
import { routes } from '../../app/config/routes';
import { ButtonLink } from '../../components/ui/Button';
import { Section } from '../../components/ui/Section';
import { Icon } from '../../components/ui/Icon';
import { useTranslations } from '../../localization/useLanguage';
import { PageIntro } from './PageIntro';
import { usePublicCopy } from './usePublicCopy';
import styles from './PublicPages.module.scss';
export function AboutPage() {
  const t = useTranslations();
  const cms = useContent<PageData>('page:about');
  const copy = usePublicCopy();
  const p = copy.about;
  return (
    <>
      <PageIntro {...p} />
      <Section>
        <div className={styles.aboutGrid}>
          <div className={styles.portrait}>
            <>
              {cms?.image ? (
                <WebsiteImage page="about" />
              ) : (
                <>
                  <Icon name="user" size={80} />
                  <p>{t.about.portrait}</p>
                </>
              )}
            </>
          </div>
          <div>
            <h2>{p.storyTitle}</h2>
            <p>{p.story}</p>
            <h2>{p.approachTitle}</h2>
            <p>{p.approach}</p>
            <ul className={styles.list}>
              {p.values.map((value) => (
                <li key={value}>{value}</li>
              ))}
            </ul>
            <ButtonLink to={routes.contact}>{copy.contactAction}</ButtonLink>
          </div>
        </div>
        <p className={styles.note}>{p.identity}</p>
      </Section>
    </>
  );
}
