import { useEffect } from 'react';
import { branding } from '../app/config/branding';
import { sv } from '../content/sv';

export function usePageMetadata(
  title: string,
  description: string = sv.meta.description,
) {
  useEffect(() => {
    document.title = `${title} | ${branding.name}`;
    const tags: Record<string, string> = {
      description,
      robots: branding.isPublicLaunch ? 'index, follow' : 'noindex, nofollow',
      'og:title': document.title,
      'og:description': description,
      'og:type': 'website',
      'og:locale': branding.locale.replace('-', '_'),
      'og:site_name': branding.name,
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
  }, [title, description]);
}
