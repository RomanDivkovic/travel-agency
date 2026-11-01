import { publicCopy } from '../../content/public';
import { useLanguage } from '../../localization/useLanguage';
export function usePublicCopy() {
  return publicCopy[useLanguage().language];
}
