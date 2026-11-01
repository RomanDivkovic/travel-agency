import { useTranslations } from '../../../localization/useLanguage';
import { images } from '../../../content/images';
import { Section } from '../../../components/ui/Section';
import styles from './HomeSections.module.scss';

export function TravelInspiration() {
  const t = useTranslations();
  return (
    <Section aria-labelledby="inspiration-title">
      <div className={`${styles.sectionHeading} ${styles.center}`}>
        <h2 id="inspiration-title">{t.inspiration.title}</h2>
        <p>{t.inspiration.description}</p>
      </div>
      <div className={styles.inspiration}>
        {t.inspiration.items.map((item) => (
          <article key={item.image}>
            <img
              {...images[item.image]}
              alt={item.alt}
              loading="lazy"
              decoding="async"
              sizes="(min-width: 768px) 30vw, 90vw"
            />
            <h3>{item.title}</h3>
            <p>{item.description}</p>
          </article>
        ))}
      </div>
      <p className={styles.imageNote}>{t.inspiration.note}</p>
    </Section>
  );
}
