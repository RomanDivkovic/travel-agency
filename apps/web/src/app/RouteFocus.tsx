import { useEffect, useRef } from 'react';
import { useLocation } from 'react-router';

export function RouteFocus() {
  const location = useLocation();
  const previousLocation = useRef(location);

  useEffect(() => {
    const previous = previousLocation.current;
    const routeChanged = previous.key !== location.key;
    const searchOnlyChanged =
      previous.pathname === location.pathname &&
      previous.hash === location.hash &&
      previous.search !== location.search;
    previousLocation.current = location;
    // Keeping wizard/filter query state in the URL must not move focus away from controls.
    if (searchOnlyChanged) return;
    const frame = window.requestAnimationFrame(() => {
      if (location.hash) {
        const target = document.getElementById(location.hash.slice(1));
        target?.scrollIntoView();
        target?.focus({ preventScroll: true });
      } else if (routeChanged) {
        window.scrollTo({ top: 0, behavior: 'instant' });
        document.getElementById('main-content')?.focus({ preventScroll: true });
      }
    });
    return () => window.cancelAnimationFrame(frame);
  }, [location]);

  return null;
}
