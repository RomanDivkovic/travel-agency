import { createContext, useContext } from 'react';
import type { CmsData, PublicContent, ServiceData, Language } from './types';
export const PreviewContext = createContext(false);
export const PreviewPageContext = createContext('');
export const WebsiteContext = createContext<PublicContent>({ documents: {} });
export function useWebsite() {
  return useContext(WebsiteContext);
}
export function useContent<T extends CmsData>(key: string) {
  return useWebsite().documents[key] as T | undefined;
}
type Mutable<T> = T extends string
  ? string
  : T extends object
    ? {
        -readonly [K in keyof T]: K extends
          'id' | 'icon' | 'image' | 'destination'
          ? T[K]
          : Mutable<T[K]>;
      }
    : T;
export function applyFields<T>(
  source: T,
  documents: PublicContent['documents'],
  prefix: 'shared' | 'public',
  language: Language,
): Mutable<T> {
  const result = structuredClone(source);
  for (const [key, value] of Object.entries(documents))
    if (key.startsWith('page:') && 'image' in value) {
      for (const [path, text] of Object.entries(value[language])) {
        const parts = path.split('.');
        if (parts.shift() !== prefix) continue;
        let obj: unknown = result;
        for (const p of parts.slice(0, -1)) {
          if (!obj || typeof obj !== 'object' || !Object.hasOwn(obj, p)) {
            obj = null;
            break;
          }
          obj = (obj as Record<string, unknown>)[p];
        }
        const last = parts.at(-1);
        if (
          last &&
          [
            'id',
            'icon',
            'image',
            'destination',
            '__proto__',
            'constructor',
            'prototype',
          ].includes(last)
        )
          continue;
        if (
          obj &&
          typeof obj === 'object' &&
          last &&
          Object.hasOwn(obj, last) &&
          typeof (obj as Record<string, unknown>)[last] === 'string'
        )
          (obj as Record<string, unknown>)[last] = text;
      }
    }
  return result as Mutable<T>;
}
export function isServiceEnabled(
  documents: PublicContent['documents'],
  code: string,
) {
  const s = documents['service:' + code] as ServiceData | undefined;
  return s?.enabled !== false;
}
