import type { Translate } from '../../content/types';
import { useWebsite, applyFields } from '../business-management/contentContext';
import { serviceCodes } from '../business-management/types';
import type { ServiceData, FaqData } from '../business-management/types';
import { publicCopy } from '../../content/public';
import { useLanguage } from '../../localization/useLanguage';
export function usePublicCopy() {
  const { language } = useLanguage();
  const { documents } = useWebsite();
  const t = applyFields<Translate<typeof publicCopy.sv>>(
    publicCopy[language],
    documents,
    'public',
    language,
  );
  for (let i = 0; i < serviceCodes.length; i++) {
    const d = documents['service:' + serviceCodes[i]] as
      ServiceData | undefined;
    const detail = t.services.details[i];
    if (d && detail) {
      detail.includes.splice(
        0,
        detail.includes.length,
        ...d[language].included.split('\n').filter(Boolean),
      );
      detail.suitable = d[language].suitable;
    }
  }
  const faqs = Object.entries(documents)
    .filter(([k]) => k.startsWith('faq:'))
    .map(([, d]) => d as FaqData)
    .filter((d) => d.enabled)
    .sort((a, b) => a.displayOrder - b.displayOrder);
  return {
    ...t,
    faq: {
      ...t.faq,
      items:
        faqs.length || Object.keys(documents).some((k) => k.startsWith('faq:'))
          ? faqs.map((d) => d[language])
          : t.faq.items,
    },
  };
}
