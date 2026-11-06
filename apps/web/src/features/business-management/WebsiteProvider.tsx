import { useEffect, useState, type ReactNode } from 'react';
import { useLocation } from 'react-router';
import { cmsApi } from './api';
import { WebsiteContext } from './contentContext';
import type { PublicContent, ServiceData, BusinessData } from './types';
import {
  useLanguage,
  languageStorageKey,
} from '../../localization/useLanguage';
export function WebsiteProvider({ children }: { children: ReactNode }) {
  const [data, setData] = useState<PublicContent>({ documents: {} });
  const { pathname } = useLocation();
  const { setLanguage } = useLanguage();
  useEffect(() => {
    const c = new AbortController();
    let timer: ReturnType<typeof setTimeout> | undefined;
    const load = () => {
      void cmsApi
        .website(c.signal)
        .then((d) => {
          if (c.signal.aborted || !d || typeof d.documents !== 'object') return;
          setData(d);
          const b = d.documents.business as BusinessData | undefined;
          try {
            if (b && !localStorage.getItem(languageStorageKey))
              setLanguage(b.defaultLanguage);
          } catch {
            /* Explicit in-memory choices still work. */
          }
          const deadlines = Object.entries(d.documents)
            .filter(([k]) => k.startsWith('service:'))
            .flatMap(([, v]) => {
              const p = (v as ServiceData).pricing;
              return [p.effectiveFrom, p.effectiveUntil];
            })
            .filter((v): v is string => !!v)
            .map((v) => new Date(v).getTime())
            .filter((v) => v > Date.now());
          if (timer) clearTimeout(timer);
          if (deadlines.length)
            timer = setTimeout(
              load,
              Math.min(
                2147483647,
                Math.max(100, Math.min(...deadlines) - Date.now() + 100),
              ),
            );
        })
        .catch(() => {
          if (!c.signal.aborted) setData({ documents: {} });
        });
    };
    const visible = () => {
      if (document.visibilityState === 'visible') load();
    };
    load();
    window.addEventListener('focus', visible);
    document.addEventListener('visibilitychange', visible);
    return () => {
      c.abort();
      if (timer) clearTimeout(timer);
      window.removeEventListener('focus', visible);
      document.removeEventListener('visibilitychange', visible);
    };
  }, [pathname, setLanguage]);
  return <WebsiteContext value={data}>{children}</WebsiteContext>;
}
