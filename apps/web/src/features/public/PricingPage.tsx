import { routes } from '../../app/config/routes';
import { ButtonLink } from '../../components/ui/Button';
import { Section } from '../../components/ui/Section';
import { useTranslations } from '../../localization/useLanguage';
import { PageIntro } from './PageIntro';
import { usePublicCopy } from './usePublicCopy';
import styles from './PublicPages.module.scss';
export function PricingPage() {
  const t = useTranslations();
  const copy = usePublicCopy();
  const p = copy.pricing;
  return (
    <>
      <PageIntro {...p} />
      <Section>
        <div className={styles.twoColumns}>
          <article className={styles.panel}>
            <span className={styles.number}>01</span>
            <h2>{p.fee}</h2>
            <p>{p.feeText}</p>
          </article>
          <article className={styles.panel}>
            <span className={styles.number}>02</span>
            <h2>{p.travel}</h2>
            <p>{p.travelText}</p>
          </article>
        </div>
        <div className={styles.quote}>
          <h2>{p.quote}</h2>
          <p>{p.quoteText}</p>
        </div>
        <p className={styles.scrollHint} id="pricing-scroll-hint">
          {p.scrollHint}
        </p>
        <div
          className={styles.tableWrap}
          role="region"
          aria-label={t.footer.services}
          aria-describedby="pricing-scroll-hint"
          tabIndex={0}
        >
          <table>
            <caption className={styles.tableCaption}>
              {t.footer.services}
            </caption>
            <thead>
              <tr>
                {p.columns.map((item) => (
                  <th scope="col" key={item}>
                    {item}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {t.services.items.map((service, i) => (
                <tr key={service.id}>
                  <th scope="row">{service.title}</th>
                  <td>{p.scopes[i]}</td>
                  <td>{p.quote}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Section>
      <Section tone="sand">
        <div className={styles.narrow}>
          <h2>{p.stepsTitle}</h2>
          <ol className={styles.list}>
            {p.steps.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ol>
          <p>{p.note}</p>
          <ButtonLink to={routes.contact}>{copy.contactAction}</ButtonLink>
        </div>
      </Section>
    </>
  );
}
