import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { Link, type LinkProps } from 'react-router';
import styles from './Button.module.scss';

type SharedProps = {
  variant?: 'primary' | 'secondary' | 'outline' | 'text';
  size?: 'small' | 'medium' | 'large';
  icon?: ReactNode;
};

export function Button({
  children,
  variant = 'primary',
  size = 'medium',
  icon,
  className = '',
  type = 'button',
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & SharedProps) {
  return (
    <button
      type={type}
      className={`${styles.button} ${styles[variant]} ${styles[size]} ${className}`}
      {...props}
    >
      {children}
      {icon}
    </button>
  );
}

export function ButtonLink({
  children,
  variant = 'primary',
  size = 'medium',
  icon,
  className = '',
  ...props
}: LinkProps & SharedProps) {
  return (
    <Link
      className={`${styles.button} ${styles[variant]} ${styles[size]} ${className}`}
      {...props}
    >
      {children}
      {icon}
    </Link>
  );
}
