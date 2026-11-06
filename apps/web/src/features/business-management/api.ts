import { adminRead, adminMutation } from '../admin/api';
import { apiClient } from '../../services/apiClient';
import type {
  ContentDetail,
  ContentItem,
  CmsData,
  PublicContent,
  MediaItem,
} from './types';
const path = (key: string) => '/cms/documents/' + encodeURIComponent(key);
export const cmsApi = {
  list: (signal?: AbortSignal) =>
    adminRead<ContentItem[]>('/cms/documents', signal),
  detail: (key: string, signal?: AbortSignal) =>
    adminRead<ContentDetail>(path(key), signal),
  save: (key: string, versionToken: string, data: CmsData) =>
    adminMutation<void>(path(key) + '/draft', 'post', { versionToken, data }),
  publish: (key: string, versionToken: string) =>
    adminMutation<void>(path(key) + '/publish', 'post', {
      versionToken,
      confirmed: true,
    }),
  restore: (key: string, versionToken: string, restoreVersion: number) =>
    adminMutation<void>(path(key) + '/restore', 'post', {
      versionToken,
      restoreVersion,
    }),
  preview: (key: string, signal?: AbortSignal) =>
    adminRead<PublicContent>('/cms/preview/' + encodeURIComponent(key), signal),
  website: async (signal?: AbortSignal) =>
    (await apiClient.get<PublicContent>('/api/public/website', { signal }))
      .data,
  media: (page: number, signal?: AbortSignal) =>
    adminRead<MediaItem[]>('/cms/media?page=' + page, signal),
  upload: (data: FormData) =>
    adminMutation<MediaItem>('/cms/media', 'post', data),
  changeMedia: (item: MediaItem) =>
    adminMutation<void>('/cms/media/' + item.id, 'patch', {
      versionToken: item.versionToken,
      altSv: item.altSv,
      altEn: item.altEn,
      decorative: item.decorative,
      archived: item.archived,
    }),
};
export function mediaUrl(id: string, admin = false) {
  return new URL(
    '/api/' + (admin ? 'admin/cms' : 'public/website') + '/media/' + id,
    apiClient.defaults.baseURL || window.location.origin,
  ).href;
}
