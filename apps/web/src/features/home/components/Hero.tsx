import { WebsiteImage } from '../../business-management/PublishedComponents';
import { useContent } from '../../business-management/contentContext';
import type { PageData } from '../../business-management/types';
import { Fragment } from 'react';
import { routes } from '../../../app/config/routes';
import { useTranslations } from '../../../localization/useLanguage';
import { heroAvifSrcSet, images } from '../../../content/images';
import { ButtonLink } from '../../../components/ui/Button';
import { Container } from '../../../components/ui/Container';
import { Icon } from '../../../components/ui/Icon';
import styles from './Hero.module.scss';

export function Hero() {
  const t = useTranslations();
  const cms = useContent<PageData>('page:home');
  return (
    <section className={styles.hero} aria-labelledby="hero-title">
      <Container className={styles.grid}>
        <div className={styles.copy}>
          <h1 id="hero-title">
            {t.hero.title.map((line, index) => (
              <Fragment key={line}>
                {index > 0 ? ' ' : ''}
                <span>{line}</span>
              </Fragment>
            ))}
          </h1>
          <p>{t.hero.description}</p>
          <div className={styles.actions}>
            <ButtonLink
              to={routes.planTrip}
              size="large"
              icon={<Icon name="arrow" size={20} />}
            >
              {t.actions.plan}
            </ButtonLink>
            <ButtonLink to={routes.services} variant="text">
              {t.actions.services}
            </ButtonLink>
          </div>
        </div>
        <div className={styles.visual}>
          <>
            {cms?.image ? (
              <WebsiteImage page="home" className={styles.coast} />
            ) : (
              <picture>
                <source
                  type="image/avif"
                  srcSet={heroAvifSrcSet}
                  sizes="(min-width: 1024px) 42vw, (min-width: 768px) 70vw, 90vw"
                />
                <img
                  {...images.coast}
                  className={styles.coast}
                  alt={t.hero.imageAlt}
                  fetchPriority="high"
                  sizes="(min-width: 1024px) 42vw, (min-width: 768px) 70vw, 90vw"
                />
              </picture>
            )}
          </>
          <img
            {...images.beach}
            className={styles.inset}
            alt=""
            sizes="22vw"
            aria-hidden="true"
          />
        </div>
      </Container>
    </section>
  );
}
