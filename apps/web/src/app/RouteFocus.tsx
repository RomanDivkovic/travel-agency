import { useEffect, useRef } from 'react';
import { useLocation } from 'react-router';

export function RouteFocus() {
  const location = useLocation();
  const previousLocation = useRef(location.key);

  useEffect(() => {
    const routeChanged = previousLocation.current !== location.key;
    previousLocation.current = location.key;
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
