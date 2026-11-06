import { ApiError } from '../../services/apiClient';
import type { useCmsCopy } from './copy';
export function contentError(e: unknown, t: ReturnType<typeof useCmsCopy>) {
  if (e instanceof ApiError) {
    if (e.status === 409) return t.conflict;
    const d = e.details;
    if (d && typeof d === 'object' && 'code' in d) {
      const codes: Record<string, string> = {
        email: t.invalidEmail,
        phone: t.invalidPhone,
        url: t.invalidUrl,
        organization: t.invalidOrganization,
        hours: t.invalidHours,
        legalReview: t.legal,
        media: t.invalidMedia,
        image: t.invalidMedia,
        size: t.file,
      };
      if (typeof d.code === 'string' && codes[d.code]) return codes[d.code];
    }
  }
  return t.error;
}
