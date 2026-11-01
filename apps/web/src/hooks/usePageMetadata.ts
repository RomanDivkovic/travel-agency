import { useEffect } from 'react';
import { useLocation } from 'react-router';
import { branding } from '../app/config/branding';
import { useLanguage, useTranslations } from '../localization/useLanguage';

export function usePageMetadata(title: string, description?: string) {
  const { language } = useLanguage();
  const t = useTranslations();
  const text = description ?? t.meta.description;
  const { pathname } = useLocation();
  useEffect(() => {
    document.title = `${title} | ${branding.name}`;
    const tags: Record<string, string> = {
      description: text,
      robots:
        !pathname.startsWith('/admin') &&
        pathname !== '/quote' &&
        pathname !== '/invoice' &&
        !pathname.startsWith('/payment/') &&
        branding.isPublicLaunch
          ? 'index, follow'
          : 'noindex, nofollow',
      'og:title': document.title,
      'og:description': text,
      'og:type': 'website',
      'og:locale': language === 'sv' ? 'sv_SE' : 'en_GB',
      'og:site_name': branding.name,
      'twitter:card': 'summary',
      'twitter:title': document.title,
      'twitter:description': text,
    };
    for (const [name, content] of Object.entries(tags)) {
      const attribute = name.startsWith('og:') ? 'property' : 'name';
      let element = document.head.querySelector<HTMLMetaElement>(
        `meta[${attribute}="${name}"]`,
      );
      if (!element) {
        element = document.createElement('meta');
        element.setAttribute(attribute, name);
        document.head.append(element);
      }
      element.content = content;
    }
    // A canonical is only emitted when an actual public HTTPS origin is configured.
    let canonical = document.head.querySelector<HTMLLinkElement>(
      'link[rel="canonical"]',
    );
    try {
      const origin = new URL(String(import.meta.env.VITE_SITE_URL));
      if (
        pathname.startsWith('/admin') ||
        pathname === '/quote' ||
        pathname === '/invoice' ||
        pathname.startsWith('/payment/') ||
        origin.protocol !== 'https:' ||
        origin.username ||
        origin.password
      )
        throw new Error('Invalid public origin');
      if (!canonical) {
        canonical = document.createElement('link');
        canonical.rel = 'canonical';
        document.head.append(canonical);
      }
      canonical.href = new URL(pathname, origin.origin).href;
    } catch {
      canonical?.remove();
    }
  }, [title, text, language, pathname]);
}
