import { routes } from '../../app/config/routes';
import { ButtonLink } from '../../components/ui/Button';
import { Section } from '../../components/ui/Section';
import { PageIntro } from './PageIntro';
import { usePublicCopy } from './usePublicCopy';
import styles from './PublicPages.module.scss';
export function FaqPage() {
  const copy = usePublicCopy();
  return (
    <>
      <PageIntro {...copy.faq} />
      <Section>
        <div className={styles.narrow}>
          <div className={styles.faq}>
            {copy.faq.items.map((item, i) => (
              <details key={i}>
                <summary>{item.question}</summary>
                <p>{item.answer}</p>
              </details>
            ))}
          </div>
          <ButtonLink to={routes.contact} variant="outline">
            {copy.contactAction}
          </ButtonLink>
        </div>
      </Section>
    </>
  );
}
