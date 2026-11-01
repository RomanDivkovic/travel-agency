import { useEffect, useRef, useState } from 'react';
import { NavLink } from 'react-router';
import { routes } from '../../app/config/routes';
import { useTranslations } from '../../localization/useLanguage';
import { ButtonLink } from '../ui/Button';
import { Container } from '../ui/Container';
import { Icon } from '../ui/Icon';
import { LanguageSwitcher } from './LanguageSwitcher';
import { BrandLogo } from './BrandLogo';
import styles from './Header.module.scss';

export function Header() {
  const t = useTranslations();
  const [open, setOpen] = useState(false);
  const header = useRef<HTMLElement>(null);
  const trigger = useRef<HTMLButtonElement>(null);

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
    return t.navigation.items.map(({ label, destination }) => (
      <NavLink
        key={destination}
        to={routes[destination]}
        onClick={onSelect}
        end
      >
        {label}
      </NavLink>
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
        <nav className={styles.desktop} aria-label={t.navigation.label}>
          {links()}
        </nav>
        <ButtonLink
          to={routes.planTrip}
          className={styles.desktopCta}
          icon={<Icon name="arrow" size={18} />}
        >
          {t.actions.plan}
        </ButtonLink>
        <LanguageSwitcher />
        <button
          ref={trigger}
          className={styles.toggle}
          type="button"
          aria-expanded={open}
          aria-controls="mobile-navigation"
          aria-label={open ? t.navigation.close : t.navigation.open}
          onClick={() => setOpen(!open)}
        >
          <Icon name={open ? 'close' : 'menu'} />
        </button>
      </Container>
      <div id="mobile-navigation" className={styles.mobile} hidden={!open}>
        <Container>
          <nav aria-label={t.navigation.mobileLabel}>
            {links(() => setOpen(false))}
          </nav>
          <ButtonLink
            to={routes.planTrip}
            onClick={() => setOpen(false)}
            icon={<Icon name="arrow" size={18} />}
          >
            {t.actions.plan}
          </ButtonLink>
        </Container>
      </div>
    </header>
  );
}
