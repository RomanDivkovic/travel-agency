import { sv } from '../../../content/sv';
import { Section } from '../../../components/ui/Section';
import styles from './HomeSections.module.scss';

export function HowItWorks() {
  return (
    <Section id="sa-fungerar-det" tone="sand" aria-labelledby="process-title">
      <div className={styles.sectionHeading}>
        <h2 id="process-title">{sv.process.title}</h2>
        <p>{sv.process.description}</p>
      </div>
      <ol className={styles.steps}>
        {sv.process.items.map((step, index) => (
          <li key={step.title}>
            <div className={styles.stepNumber} aria-hidden="true">
              <span>{String(index + 1).padStart(2, '0')}</span>
            </div>
            <h3>{step.title}</h3>
            <p>{step.description}</p>
          </li>
        ))}
      </ol>
      <p className={styles.processNote}>{sv.process.note}</p>
    </Section>
  );
}
