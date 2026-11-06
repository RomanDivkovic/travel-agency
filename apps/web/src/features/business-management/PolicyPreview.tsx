import { useContent } from './contentContext';
import { useLanguage } from '../../localization/useLanguage';
import type { PolicyData } from './types';
import { Section } from '../../components/ui/Section';
export function PolicyPreview({ documentKey }: { documentKey: string }) {
  const data = useContent<PolicyData>(documentKey);
  const { language } = useLanguage();
  return data ? (
    <Section>
      <h1>{data[language].title}</h1>
      <p>{data.effectiveFrom}</p>
      <div style={{ whiteSpace: 'pre-wrap' }}>{data[language].content}</div>
    </Section>
  ) : null;
}
