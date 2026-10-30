import type { HTMLAttributes } from 'react';
import styles from './Card.module.scss';

export function Card({
  className = '',
  ...props
}: HTMLAttributes<HTMLElement>) {
  return <article className={`${styles.card} ${className}`} {...props} />;
}
