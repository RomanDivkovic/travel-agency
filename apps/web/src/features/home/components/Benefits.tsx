import { Fragment } from 'react';
import { sv } from '../../../content/sv';
import { Icon } from '../../../components/ui/Icon';
import { Section } from '../../../components/ui/Section';
import styles from './HomeSections.module.scss';

export function Benefits() {
  return (
    <Section aria-labelledby="benefits-title">
      <div className={styles.benefits}>
        <h2 id="benefits-title">
          {sv.benefits.title.map((line, index) => (
            <Fragment key={line}>
              {index > 0 ? ' ' : ''}
              <span>{line}</span>
            </Fragment>
          ))}
        </h2>
        <ul>
          {sv.benefits.items.map((item) => (
            <li key={item.title}>
              <Icon name={item.icon} size={30} />
              <h3>{item.title}</h3>
              <p>{item.description}</p>
            </li>
          ))}
        </ul>
      </div>
    </Section>
  );
}
