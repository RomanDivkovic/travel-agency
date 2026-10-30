import type { HTMLAttributes } from 'react';
import { Container } from './Container';
import styles from './Section.module.scss';

export function Section({
  children,
  className = '',
  tone = 'default',
  ...props
}: HTMLAttributes<HTMLElement> & { tone?: 'default' | 'sand' | 'navy' }) {
  return (
    <section
      className={`${styles.section} ${styles[tone]} ${className}`}
      tabIndex={-1}
      {...props}
    >
      <Container>{children}</Container>
    </section>
  );
}
