import { useEffect, useRef, useState } from 'react';
import { Link, useLocation } from 'react-router';
import { routes } from '../../app/config/routes';
import { sv } from '../../content/sv';
import { ButtonLink } from '../ui/Button';
import { Container } from '../ui/Container';
import { Icon } from '../ui/Icon';
import { BrandLogo } from './BrandLogo';
import styles from './Header.module.scss';

export function Header() {
  const [open, setOpen] = useState(false);
  const header = useRef<HTMLElement>(null);
  const trigger = useRef<HTMLButtonElement>(null);
  const location = useLocation();

  useEffect(() => {
    const close = () => setOpen(false);
    const outside = (event: PointerEvent) => {
      if (
        event.target instanceof Node &&
        !header.current?.contains(event.target)
      )
        close();
    };
    window.addEventListener('resize', close);
    document.addEventListener('pointerdown', outside);
    return () => {
      window.removeEventListener('resize', close);
      document.removeEventListener('pointerdown', outside);
    };
  }, []);

  function links(onSelect?: () => void) {
    return sv.navigation.items.map(({ label, destination }) => (
      <Link
        key={destination}
        to={routes[destination]}
        onClick={onSelect}
        aria-current={
          location.pathname === routes.home &&
          ((destination === 'home' && !location.hash) ||
            routes[destination] === `/${location.hash}`)
            ? 'location'
            : undefined
        }
      >
        {label}
      </Link>
    ));
  }

  return (
    <header
      ref={header}
      className={styles.header}
      onKeyDown={(event) => {
        if (event.key === 'Escape' && open) {
          setOpen(false);
          trigger.current?.focus();
        }
      }}
      onBlur={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget)) setOpen(false);
      }}
    >
      <Container className={styles.row}>
        <BrandLogo />
        <nav className={styles.desktop} aria-label={sv.navigation.label}>
          {links()}
        </nav>
        <ButtonLink
          to={routes.planTrip}
          className={styles.desktopCta}
          icon={<Icon name="arrow" size={18} />}
        >
          {sv.actions.plan}
        </ButtonLink>
        <button
          ref={trigger}
          className={styles.toggle}
          type="button"
          aria-expanded={open}
          aria-controls="mobile-navigation"
          aria-label={open ? sv.navigation.close : sv.navigation.open}
          onClick={() => setOpen(!open)}
        >
          <Icon name={open ? 'close' : 'menu'} />
        </button>
      </Container>
      <div id="mobile-navigation" className={styles.mobile} hidden={!open}>
        <Container>
          <nav aria-label={sv.navigation.mobileLabel}>
            {links(() => setOpen(false))}
          </nav>
          <ButtonLink
            to={routes.planTrip}
            onClick={() => setOpen(false)}
            icon={<Icon name="arrow" size={18} />}
          >
            {sv.actions.plan}
          </ButtonLink>
        </Container>
      </div>
    </header>
  );
}
